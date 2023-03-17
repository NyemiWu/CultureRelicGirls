using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat {

	public class EnemyDasher:EntityEnemy {

		float timeSinceAttack;
		int chargeSign;//-1�� 1��
		protected override void StartAttack() {
			base.StartAttack();
			timeSinceAttack=0;
			chargeSign=targetX>transform.position.x ? 1 : -1;
			animator.SetTrigger("DashAttack");
		}

		float moveSpeed = 1;
		float chargeSpeed = 5;
		float signatureEnd = 0.6f;
		float preChargeEnd = 0.5f;
		float chargeEnd = 1.1f;
		float animationEnd = 1.1f;

		protected override void StateAttack() {
			base.StateAttack();
			timeSinceAttack+=Time.deltaTime;
			float distanceToTarget = Mathf.Abs(targetX - transform.position.x);
            if (distanceToTarget < 8f && distanceToTarget > 6.5f) {
                if (timeSinceAttack < signatureEnd) {
                    velocity = Vector2.zero;
                } else if (timeSinceAttack < preChargeEnd) {
                    velocity = Vector2.right * chargeSign * moveSpeed;
                } else if (timeSinceAttack < chargeEnd) {
                    velocity = Vector2.right * chargeSign * chargeSpeed;
                    UpdateContactDamage();
                } else if (timeSinceAttack < animationEnd) {
                    velocity = Vector2.right * chargeSign * moveSpeed;
                } else {
                    StartMove();
                }
                animator.ResetTrigger("hit");
			
            } else {
                StartMove();
            }

		}

	}

}
