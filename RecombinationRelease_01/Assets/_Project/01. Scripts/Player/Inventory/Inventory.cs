using Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    #region Variables
    [Header("Inventory")]
    [SerializeField] private PlayerController owner;
    [SerializeField] private List<PartBase> baseParts = new List<PartBase>();
    private Dictionary<EPartType, List<PartBase>> _items = new Dictionary<EPartType, List<PartBase>>();
    private Dictionary<EPartType, List<PartBase>> _equippedItems = new();
    private int partIndex = 0;

    [Header("Mesh and Bone Data")]
    [SerializeField] private Transform boneRoot;
    [SerializeField] private Transform meshRoot;
    private Dictionary<string, PartBase> _parts = new();    // Mesh Root에 자식으로 있는 모든 파츠
    private Dictionary<EPartType, List<string>> _boneList = new();
    private Dictionary<EPartType, Dictionary<string, Transform>> _boneMap = new();
    private Dictionary<EPartType, List<string>> _laserBoneList = new();
    private Dictionary<EPartType, Dictionary<string, Transform>> _laserBoneMap = new();
    private Dictionary<EPartType, List<string>> _rapidBoneList = new();
    private Dictionary<EPartType, Dictionary<string, Transform>> _rapidBoneMap = new();
    private Dictionary<EPartType, List<string>> _heavyBoneList = new();
    private Dictionary<EPartType, Dictionary<string, Transform>> _heavyBoneMap = new();
    private Dictionary<EPartType, List<string>> _subBoneList = new();
    private Dictionary<EPartType, Dictionary<string, Transform>> _subBoneMap = new();
    #endregion

    #region Properties
    public Dictionary<EPartType, List<PartBase>> Items
    {
        get { return _items; }
    }

    public Dictionary<EPartType, List<PartBase>> EquippedItems
    {
        get { return _equippedItems; }
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        foreach (EPartType partType in Enum.GetValues(typeof(EPartType)))
        {
            _equippedItems.Add(partType, new List<PartBase>());
        }

        for (int i = 0; i < Enum.GetNames(typeof(EPartType)).Length; ++i)
        {
            _items.Add((EPartType)(1 << i), new List<PartBase>());
            _boneList.Add((EPartType)(1 << i), new List<string>());
            _boneMap.Add((EPartType)(1 << i), new Dictionary<string, Transform>());
            _laserBoneList.Add((EPartType)(1 << i), new List<string>());
            _laserBoneMap.Add((EPartType)(1 << i), new Dictionary<string, Transform>());
            _rapidBoneList.Add((EPartType)(1 << i), new List<string>());
            _rapidBoneMap.Add((EPartType)(1 << i), new Dictionary<string, Transform>());
            _heavyBoneList.Add((EPartType)(1 << i), new List<string>());
            _heavyBoneMap.Add((EPartType)(1 << i), new Dictionary<string, Transform>());
            _subBoneList.Add((EPartType)(1 << i), new List<string>());
            _subBoneMap.Add((EPartType)(1 << i), new Dictionary<string, Transform>());
        }

        foreach (EPartType partType in Enum.GetValues(typeof(EPartType)))
        {
            _boneList[partType] = Resources.Load<CharacterBoneData>($"Bone/Player{partType.ToString()}BoneData").boneNames;
            _laserBoneList[partType] = Resources.Load<CharacterBoneData>($"Bone/Laser{partType.ToString()}BoneData").boneNames;
            _rapidBoneList[partType] = Resources.Load<CharacterBoneData>($"Bone/Rapid{partType.ToString()}BoneData").boneNames;
            _heavyBoneList[partType] = Resources.Load<CharacterBoneData>($"Bone/Heavy{partType.ToString()}BoneData").boneNames;

            foreach (Transform bone in boneRoot.GetComponentsInChildren<Transform>())
            {
                if (_boneList[partType].Contains(bone.name))
                {
                    _boneMap[partType].Add(bone.name, bone);
                }

                if (_laserBoneList[partType].Contains(bone.name))
                {
                    _laserBoneMap[partType].Add(bone.name, bone);
                }

                if (_rapidBoneList[partType].Contains(bone.name))
                {
                    _rapidBoneMap[partType].Add(bone.name, bone);
                }

                if (_heavyBoneList[partType].Contains(bone.name))
                {
                    _heavyBoneMap[partType].Add(bone.name, bone);
                }
            }

            CharacterBoneData subBoneData = Resources.Load<CharacterBoneData>($"Bone/Sub{partType.ToString()}BoneData");
            if (subBoneData)
            {
                _subBoneList[partType] = subBoneData.boneNames;
                foreach (Transform bone in boneRoot.GetComponentsInChildren<Transform>())
                {
                    if (_subBoneList[partType].Contains(bone.name))
                    {
                        _subBoneMap[partType].Add(bone.name, bone);
                    }
                }
            }
        }

        for (int i = 0; i < meshRoot.childCount; ++i)
        {
            PartBase target = meshRoot.GetChild(i).GetComponent<PartBase>();
            if (target != null)
            {
                _parts[meshRoot.GetChild(i).name] = target;
                SetPartBone(target);
                if (target.MeshType == EPartMeshType.Static)
                {
                    --i;
                }

                target.Init(owner);
                target.gameObject.SetActive(false);

                // 테스트용으로 MeshRoot에 있는 모든 파츠를 인벤토리에 추가
                GetItem(target);
            }
        }

        foreach (PartBase part in baseParts)
        {
            // GetItem(part);
            EquipItem(part);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (partIndex == 0) return;

            partIndex = 0;
            EquipItem(_items[EPartType.Shoulder][partIndex]);
            EquipItem(_items[EPartType.ArmL][partIndex]);
            EquipItem(_items[EPartType.ArmR][partIndex]);
            EquipItem(_items[EPartType.Legs][partIndex]);
            EquipItem(_items[EPartType.Back][partIndex]);
            EquipItem(_items[EPartType.Mask][partIndex]);

            // 임시로 파츠 전체 교체 시 특정 파츠 카메라로 변경
            // To-do: R&D가 필요하나, 캐릭터 콜라이더 크기에 따라 카메라 위치를 조정하는 등의 조치 필요
            owner.FollowCamera.CurrentCameraState = ECameraState.Normal;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (partIndex == 1) return;

            partIndex = 1;
            EquipItem(_items[EPartType.Shoulder][partIndex]);
            EquipItem(_items[EPartType.ArmL][partIndex]);
            EquipItem(_items[EPartType.ArmR][partIndex]);
            EquipItem(_items[EPartType.Legs][partIndex]);
            EquipItem(_items[EPartType.Back][partIndex]);
            EquipItem(_items[EPartType.Mask][partIndex]);

            owner.FollowCamera.CurrentCameraState = ECameraState.Hover;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (partIndex == 2) return;

            partIndex = 2;
            EquipItem(_items[EPartType.Shoulder][partIndex]);
            EquipItem(_items[EPartType.ArmL][partIndex]);
            EquipItem(_items[EPartType.ArmR][partIndex]);
            EquipItem(_items[EPartType.Legs][partIndex]);
            EquipItem(_items[EPartType.Back][partIndex]);
            EquipItem(_items[EPartType.Mask][partIndex]);

            owner.FollowCamera.CurrentCameraState = ECameraState.Roller;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (partIndex == 3) return;

            partIndex = 3;
            EquipItem(_items[EPartType.Shoulder][partIndex]);
            EquipItem(_items[EPartType.ArmL][partIndex]);
            EquipItem(_items[EPartType.ArmR][partIndex]);
            EquipItem(_items[EPartType.Legs][partIndex]);
            EquipItem(_items[EPartType.Back][partIndex]);
            EquipItem(_items[EPartType.Mask][partIndex]);

            owner.FollowCamera.CurrentCameraState = ECameraState.Caterpillar;
        }

        // GUI 갱신 스크립트
        GUIManager.Instance.SetLeftPartText(_items[EPartType.ArmL][partIndex].name);
        GUIManager.Instance.SetRightPartText(_items[EPartType.ArmR][partIndex].name);
        GUIManager.Instance.SetLegsText(_items[EPartType.Legs][partIndex].name);
    }
    #endregion

    #region Public Methods
    public void GetItem(PartBase newItem)
    {
        if (!_items[newItem.PartType].Contains(newItem))
        {
            _items[newItem.PartType].Add(newItem);
        }
    }

    public bool RemoveItem(PartBase removeItem)
    {
        return _items[removeItem.PartType].Remove(removeItem);
    }
	
	// TODO: 파츠별,파리미터별 적용 방식 디테일한 논의 필요
    public void EquipItem(PartBase equipItem)
    {
        if (!_items[equipItem.PartType].Contains(equipItem)) return;

        foreach (var part in _equippedItems[equipItem.PartType])
        {
            part.FinishActionForced();
            owner.Stats.RemoveModifier(part);
            part.gameObject.SetActive(false);
            foreach (Transform child in part.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        _equippedItems[equipItem.PartType].Clear();

        // 동시 장착할 파츠들 찾기 (이름에 따라 필터링 가능)
        var sameTypeParts = _items[equipItem.PartType]
            .Where(x => x.AttackType == equipItem.AttackType)   // 예시: GroupKey로 분류, 필요에 따라 본인의 기준으로 변경
            .ToList();

        // 여러 파츠 모두 장착
        foreach (var part in sameTypeParts)
        {
            part.gameObject.SetActive(true);
            foreach (Transform child in part.transform)
            {
                child.gameObject.SetActive(true);
            }
            _equippedItems[equipItem.PartType].Add(part);
        }

        owner.SetPartStat(equipItem);

        PartBaseLegs legs = sameTypeParts.OfType<PartBaseLegs>().FirstOrDefault();
        if (legs != null)
        {
            // 다리 파츠 (애니메이션 변경)
            owner.SetOvrrideAnimator(legs.LegsAnimType);
        }
    }

    public override string ToString()
    {
        string ownerName = owner != null ? owner.name : "Null";
        string basePartsNames = baseParts != null && baseParts.Count > 0
            ? string.Join(", ", baseParts.ConvertAll(p => p.name))
            : "None";

        string itemsSummary = string.Join(", ", _items.Select(kvp =>
            $"{kvp.Key}: [{string.Join(", ", kvp.Value.ConvertAll(p => p.name))}]"));

        string equippedSummary = string.Join(", ", _equippedItems.Select(kvp =>
            $"{kvp.Key}: [{string.Join(", ", kvp.Value.ConvertAll(p => p.name))}]"));

        return $"Inventory:\n" +
               $"  Owner: {ownerName}\n" +
               $"  BaseParts: [{basePartsNames}]\n" +
               $"  Items:\n    {itemsSummary.Replace(", ", "\n    ")}\n" +
               $"  EquippedItems:\n    {equippedSummary.Replace(", ", "\n    ")}";
    }
    #endregion

    #region Private Methods
    private void SetPartBone(PartBase part)
    {
        EPartMeshType meshType = part.MeshType;
        switch (meshType)
        {
            case EPartMeshType.Skinned:
                SetSkinnedMeshBone(part);
                break;
            case EPartMeshType.Static:
                SetStaticMeshBone(part);
                break;
        }
    }

    private void SetSkinnedMeshBone(PartBase part)
    {
        SkinnedMeshRenderer smr = part.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError($"SkinnedMeshRenderer not found on {part.name}. Please ensure it has a SkinnedMeshRenderer component.");
            return;
        }

        List<Transform> meshTransforms = new List<Transform>();
        if (part.gameObject.name.Contains("Sub"))
        {
            for (int i = 0; i < smr.bones.Length; ++i)
            {
                meshTransforms.Add(_subBoneMap[part.PartType][_subBoneList[part.PartType][i]]);
            }
        }
        else if (part.gameObject.name.Contains("Heavy"))
        {
            for (int i = 0; i < smr.bones.Length; ++i)
            {
                meshTransforms.Add(_heavyBoneMap[part.PartType][_heavyBoneList[part.PartType][i]]);
            }
        }
        else if (part.gameObject.name.Contains("Laser"))
        {
            for (int i = 0; i < smr.bones.Length; ++i)
            {
                meshTransforms.Add(_laserBoneMap[part.PartType][_laserBoneList[part.PartType][i]]);
            }
        }
        else if (part.gameObject.name.Contains("Rapid"))
        {
            for (int i = 0; i < smr.bones.Length; ++i)
            {
                meshTransforms.Add(_rapidBoneMap[part.PartType][_rapidBoneList[part.PartType][i]]);
            }
        }
        else
        {
            for (int i = 0; i < smr.bones.Length; ++i)
            {
                meshTransforms.Add(_boneMap[part.PartType][_boneList[part.PartType][i]]);
            }
        }
        smr.bones = meshTransforms.ToArray();
        //smr.rootBone = rootBone;
    }

    private void SetStaticMeshBone(PartBase part)
    {
        MeshRenderer mr = part.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            SkinnedMeshRenderer smr = part.GetComponent<SkinnedMeshRenderer>();
            if (smr == null) return;

            part.transform.SetParent(smr.rootBone);
        }
        else
        {
            part.transform.SetParent(boneRoot);
        }

        part.transform.localPosition = part.StaticOffset;
        part.transform.localRotation = Quaternion.Euler(part.StaticRotation);
    }
    #endregion
}
