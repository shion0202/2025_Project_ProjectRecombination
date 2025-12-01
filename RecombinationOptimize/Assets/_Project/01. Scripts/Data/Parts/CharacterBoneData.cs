using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터 본 데이터를 저장하기 위한 스크립터블 오브젝트 클래스
[CreateAssetMenu(menuName = "Scriptable Object/Character Bone Data", order = 10)]
public class CharacterBoneData : ScriptableObject
{
    public List<string> boneNames = new List<string>();
}
