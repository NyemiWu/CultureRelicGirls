using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Museum {

	/// <summary>
	/// 每个建筑物对应的数值
	/// </summary>
	[CreateAssetMenu(menuName = "自定/建筑数值")]
	public class BuildingData:ScriptableObject {

		public static Dictionary<int,BuildingData> datas;
		private void OnEnable() {
			datas.Add(id,this);
		}

		public BuildingLevelData[] levels;
		[field: SerializeField] public int maxLevel { get; private set; }
		[field: SerializeField] public int id { get; private set; }

	}


	/// <summary>
	/// 每个建筑物随等级变化的数值
	/// </summary>
	[System.Serializable]
	public struct BuildingLevelData {

		/// <summary>
		/// 升级消耗材料的量
		/// </summary>
		public int levelUpCostMaterial;
		/// <summary>
		/// 升级消耗意识物质的量
		/// </summary>
		public int levelUpCostSentienceMatter;
		/// <summary>
		/// 升级消耗的时间
		/// </summary>
		public System.TimeSpan levelUpTime { get { return new System.TimeSpan(levelUpHour,levelUpMinute,0); } }
		public int levelUpHour;
		public int levelUpMinute;

	}

}