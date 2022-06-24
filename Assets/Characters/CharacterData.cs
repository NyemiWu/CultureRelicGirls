using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "自定/角色数值")]
public class CharacterData:ScriptableObject {

	public static Dictionary<int,CharacterData> datas = new Dictionary<int,CharacterData>();
	private void OnEnable() {
		datas.Add(id,this);
	}

	[field: SerializeField] public int id { get; private set; }
	[field: SerializeField] public int maxLevel { get; private set; }
	[field: SerializeField] public GameObject combatPrefab { get; private set; }
	[field: SerializeField] public float healCostPerHp { get; private set; }
	[field: SerializeField] public float healTimPerHpInSecond { get; private set; }
	[field: SerializeField] public Sprite sprite { get; private set; } //战斗中的精灵
	[field: SerializeField] public Sprite picture { get; private set; }//实物图片
	public CharacterLevelData[] levels;

}

[System.Serializable]
public struct CharacterLevelData {
	/// <summary>
	/// 升级消耗意识物质的量
	/// </summary>
	public int levelUpCost;
	/// <summary>
	/// 升级消耗的时间
	/// </summary>
	public System.TimeSpan levelUpCostTime { get { return new System.TimeSpan(levelUpHour,levelUpMinute,0); } }
	public int levelUpHour;
	public int levelUpMinute;
	public System.TimeSpan levelUpTimeTime { get { return new System.TimeSpan(levelUpTimeHour,levelUpTimeMinute,0); } }
	public int levelUpTimeHour;
	public int levelUpTimeMinute;

	public int hpMax;
	public int power;

}