using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat {

	public class EntityEnemy:EntityBase {

		[field: SerializeField] public int enemyId { get; private set; }
		[SerializeField] protected float wakeUpDistanceFront;
		[SerializeField] protected float wakeUpDistanceBack;
		[SerializeField] int sentienceMatterReward;

		[SerializeField] bool startRight;

		[SerializeField] protected bool doingDamage;
		[SerializeField] Collider2D damageBox;

		[Tooltip("瞄准的友方角色位置, 若这个单位属于友方阵营或此值不在0~2范围内则瞄准最近的角色")]
		[SerializeField] int targetIndex;

		[Tooltip("尸体每帧动画持续的时间")]
		[SerializeField] float corpseTimePerFrame;
		[Tooltip("尸体动画")]
		[SerializeField] Sprite[] corpseAnimation;

		[Tooltip("死亡时掉落意识晶体的量")]
		[SerializeField] int moneyDrop;

		#region 攻击目标

		protected override void UpdateTarget() {
			base.UpdateTarget();

			if(isFriendly||targetIndex<0||targetIndex>=3) return;
			target=null;
			for(int i = targetIndex;i<3;i++) {
				if(EntityFriendly.friendlyList[i]) target=EntityFriendly.friendlyList[i];
			}

		}

		#endregion
		#region 攻击动画/攻击状态

		[SerializeField] protected float attackChance = 1;        //移动结束后进入攻击状态的概率
		[SerializeField] protected AttackStateData[] attacks;     //包含所有攻击状态的列表

		HashSet<EntityBase> targetHit = new HashSet<EntityBase>();

		//播放攻击动画

		public float 攻击动画移动速度;
		protected float overrideSpeed {
			get => 攻击动画移动速度;
			set => 攻击动画移动速度=value;
		}

		protected int attackIndex;

		protected readonly static List<AttackStateData> attackStatesBuffer = new List<AttackStateData>();
		protected readonly static List<AttackStateTransistion> transitionBuffer = new List<AttackStateTransistion>();

		protected virtual void StartRandomAttack() {
			float weightTotal = 0;
			float distanceToTarget = this.distanceToTarget;

			//找出可能的转移目标
			attackStatesBuffer.Clear();
			foreach(var i in attacks) {
				if(i.maxDistance<distanceToTarget||i.minDistance>distanceToTarget) continue;
				weightTotal+=i.startWeight;
				attackStatesBuffer.Add(i);
			}

			//选择实际的转移目标(攻击动画)
			int targetIndex = ChooseByWeight.Work((int a) => attackStatesBuffer[a].startWeight,attackStatesBuffer.Count);
			AttackStateData targetState = targetIndex<0 ? null : attackStatesBuffer[targetIndex];

			/*
			float randomFactor = Random.Range(0,weightTotal);
			foreach(var i in attackStatesBuffer) {
				randomFactor-=i.startWeight;
				if(randomFactor<=Mathf.Epsilon) {
					targetState=i;
					break;
				}
			}
			*/

			//判断要转移到攻击动画还是行走
			if(Utility.Chance(1-attackChance)||targetState==null) {
				//继续行走
				StartMove();
			} else {
				//进行攻击
				StartAttack(targetState.id);
			}

		}
		protected virtual void StartAttack(int attackIndex) {
			direction=targetX>transform.position.x ? Direction.right : Direction.left;
			this.attackIndex=attackIndex;
			animator.SetFloat($"attackType",attackIndex);
			animator.SetTrigger($"attackStart");

			//animator.SetTrigger($"attack{attackIndex}");
			currensState=StateAttack;
			nameHashSet=false;
			targetHit.Clear();
		}
		protected virtual void StateAttack() {

			AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
			if(!nameHashSet) {
				nameHashSet=true;
				nameHash=state.fullPathHash;
			}
			if(nameHash!=state.fullPathHash) AttackEnd();

			velocity=overrideSpeed*Direction.GetVector(direction);
			overrideSpeed=0;
			if(doingDamage) UpdateContactDamage();
			else targetHit.Clear();

			//animator.ResetTrigger("stagger");
			animator.ResetTrigger("hit");

		}

		//在代码中提前结束攻击的接口
		protected virtual void EndAttackPremature() {
			animator.SetTrigger("attackEnd");
			StartMove();
		}

		//通过动画在攻击结束时调用
		public virtual void AttackEnd() {
			transitionBuffer.Clear();
			float distanceToTarget = this.distanceToTarget;
			AttackStateData currentAttack = attacks[attackIndex];
			float weightTotal = 0;

			//统计可用的目标状态

			foreach(var i in currentAttack.transitionList) {
				switch(i.type) {
				case AttackStateTransistionType.Attack:
					if(distanceToTarget<attacks[i.attackId].minDistance||distanceToTarget>attacks[i.attackId].maxDistance) break;
					weightTotal+=i.weight;
					transitionBuffer.Add(i);
					break;

				case AttackStateTransistionType.Move:
					weightTotal+=i.weight;
					transitionBuffer.Add(i);
					break;

				}


			}

			//判断最终目标
			int targetIndex = ChooseByWeight.Work((a) => transitionBuffer[a].weight,transitionBuffer.Count);
			AttackStateTransistion transistion = transitionBuffer[targetIndex];
			/*
			float randomFactor = Random.Range(0,weightTotal);
			foreach(var i in transitionBuffer) {
				randomFactor-=i.weight;
				if(randomFactor<=Mathf.Epsilon) {
					transistion=i;
					break;
				}
			}
			*/

			if(transistion==null||transistion.type==AttackStateTransistionType.Move) StartMove();
			else StartAttack(transistion.attackId);

		}
		#endregion

		protected override float distanceToTarget => Mathf.Abs(transform.position.x-targetX);

		protected override void Start() {
			base.Start();
			direction=startRight ? Direction.right : Direction.left;
			StartInactive();
			poise=poiseMax;
			for(int i = 0;i<attacks.Length;i++) attacks[i].id=i;
			groundY=spriteRenderer.transform.position.y-transform.parent.position.y;
		}

		protected override void FixedUpdate() {
			base.FixedUpdate();
			timeAfterKnockback+=Time.deltaTime;
			if(timeAfterKnockback>poiseRegenDelay) poise+=poiseRegen*Time.deltaTime;
			if(poise>poiseMax) poise=poiseMax;
			if(poise<=0) StartStagger();
		}

		//返回是否命中敌人
		protected virtual bool UpdateContactDamage() {
			//碰撞伤害
			if(isKnockbacked) return false;

			bool result = false;

			int cnt = damageBox.Cast(Vector2.left,Utility.raycastBuffer,Mathf.Abs(velocity.x)*Time.deltaTime);

			for(int i = 0;i<cnt;i++) {

				RaycastHit2D hit = Utility.raycastBuffer[i];
				EntityBase other = hit.collider.GetComponent<EntityBase>();
				if(other&&other.isFriendly) {

					if(targetHit.Contains(other)) continue;
					targetHit.Add(other);

					DamageModel damage = GetDamage();

					if(other.isKnockbacked) damage.amount=0;

					damage.direction=direction;
					damage.damageType=DamageType.Contact;
					other.Damage(damage);
					result=true;
				}
			}

			return result;
		}

		protected virtual void StartInactive() {
			currensState=StateInactive;
		}
		protected virtual void StateInactive() {
			float x = transform.position.x;
			bool toActive = false;

			float sightLeft = x-(direction==Direction.right ? wakeUpDistanceBack : wakeUpDistanceFront);
			float sightRight = x+(direction==Direction.left ? wakeUpDistanceBack : wakeUpDistanceFront);

			foreach(var i in entities) {
				if(!i) continue;
				if(i.isFriendly==isFriendly) continue;
				float pos = i.transform.position.x;
				if(pos<sightRight&&pos>sightLeft) toActive=true;
			}

			if(toActive) StartMove();
		}

		#region 移动

		[Tooltip("单位选择移动时追求的与目标距离的最小值\n单位在进入移动状态时会在此值与最大值之间随机选择一个值作为追求的与目标距离, 在与目标的距离达到追求的距离后会进行一次状态转移")]
		[SerializeField] protected float targetDistanceMin;
		[Tooltip("单位选择移动时追求的与目标距离的最大值")]
		[SerializeField] protected float targetDistanceMax;

		protected float targetDistance;

		float moveTime;
		protected override void StartMove() {
			base.StartMove();
			moveTime=0;
			targetDistance=Random.Range(targetDistanceMin,targetDistanceMax);
		}

		protected override void StateMove() {

			moveTime+=Time.deltaTime;
			if(moveTime>2) StartMove();

			Vector2 position = transform.position;
			float moveTargetX;
			if(targetX>position.x) {
				//向右	
				moveTargetX=targetX-targetDistance;
			} else {
				//向左
				moveTargetX=targetX+targetDistance;
			}

			//确定速度
			Vector2 targetVelocity = (moveTargetX>position.x ? Vector2.right : Vector2.left)*speedBuff*maxSpeed;
			direction=(targetX>position.x) ? Direction.right : Direction.left;
			float deltaSpeed = acceleration*((speedBuff+1)*0.5f)*Time.deltaTime;
			velocity=Vector2.MoveTowards(velocity,targetVelocity,deltaSpeed);

			position+=velocity*Time.deltaTime;
			transform.position=position;

			if(Mathf.Abs(position.x-moveTargetX)<0.2f) {
				//结束移动
				StartRandomAttack();
			}

		}

		public void 移动事件_EndMove() {
			StartRandomAttack();
		}

		#endregion

		#region 韧性机制

		[Tooltip("接受击退时受到的击退力和韧性伤害会被减去这个值")]
		[SerializeField] protected float knockbackDefense;
		[Tooltip("最大韧性值")]
		[SerializeField] protected float poiseMax;
		[Tooltip("韧性值恢复速率")]
		[SerializeField] protected float poiseRegen;
		[Tooltip("韧性值恢复冷却")]
		[SerializeField] protected float poiseRegenDelay;

		protected float poise;
		protected float staggeredTime;
		protected float timeAfterKnockback;

		public override void DoKnockback(float knockback,int direction) {
			float actualKnockback = Mathf.Max(0,knockback-knockbackDefense);
			poise-=actualKnockback;
			timeAfterKnockback=0;
			base.DoKnockback(knockback,direction);
		}

		protected virtual void StartStagger() {
			currensState=StateStagger;
			animator.SetTrigger("stagger");
			nameHashSet=false;
		}
		protected virtual void StateStagger() {
			var state = animator.GetCurrentAnimatorStateInfo(0);
			if(!nameHashSet) {
				nameHash=state.fullPathHash;
				nameHashSet=true;
			}

			velocity=Vector2.zero;
			poise=poiseMax;
			if(state.fullPathHash!=nameHash) StartMove();

		}

		#endregion

		protected float groundY;
		protected override void OnDeath() {
			base.OnDeath();
			if(corpseAnimation!=null&&corpseAnimation.Length!=0) {
				EnemyCorpse newCorpse = EnemyCorpse.Create(corpseAnimation,transform.position,corpseTimePerFrame,direction,groundY);
				newCorpse.gameObject.transform.parent=room.transform;
				CombatController.instance.rewardSm+=sentienceMatterReward;
				Destroy(gameObject);
			}

			PlayerData.PlayerDataRoot.smCount+=moneyDrop;

		}

	}

}