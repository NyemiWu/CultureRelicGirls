using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat {

	public class LongRangeBase:EntityFriendly {

		protected override void ChargeStart() {
			base.ChargeStart();
			ChargeStartAction();
		}

		virtual protected void ChargeStartAction(){
			EntityBase target = GetNearestTarget();
			Attack(target);
			UpdateAttack();
		}

	}

}