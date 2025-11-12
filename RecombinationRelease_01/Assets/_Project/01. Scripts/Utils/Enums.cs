using System;

public enum EEventType
{
    Interaction,
}

// Unit(플레이어, 몬스터) 스탯 종류
public enum EStatType
{
    ID,
    Name,
    MaxHp,
    Damage,
    WalkSpeed,
    RunSpeed,
    Defence,
    MaxDetectiveRange,                  // 범위 (예: 적을 인식하는 거리)
    MinDetectiveRange,
    PartType,               // 파츠 타입 (예: 등/어깨, 팔, 다리)
    IntervalBetweenShots,   // 탄 발사 간격 시간값
    AddHp,
    AddDefence,
    AddMoveSpeed,
    DamageReductionRate,    // 피해 감소 값 (%)
    CooldownReduction,      // 쿨타임 감소 값
    StatusEffectType,       // 상태이상 타입 (예: 중독, 기절 등)

    // 몬스터 스킬
    SkillType,            // 스킬 타입 (예: 근거리 공격, 원거리 공격, 방어 등)
    CooldownTime,         // 스킬 쿨타임
    AnimSpeed,            // 애니메이션 속도
    Range,                // 스킬 사거리

    IdArray,             // ID 배열 (예: 아이템을 장착할 때 사용)
    // TODO: 스킬은 다시 정의해야함
    // SkillDamage,
    // SkillSpeed,
    // SkillCount,
    // SkillCooldown,
    // CooldownDecrease,
    // Ailment,
}

// 스탯 연산 방식
public enum EStackType
{
    Flat,           // 고정값 (+10 + 10)
    PercentAdd,     // 합연산 (+10% + 10%)
    PercentMul      // 곱연산 (×1.1 × 1.1)
}

// 회복 범위
public enum EHealRange
{
    All = 0,
    Body = 1,
    Part = 2,
}

// 회복 연산 방식
public enum EHealType
{
    Flat = 0,
    Percentage = 1,
}

// 캐릭터 상태
[Flags]
public enum EPlayerState
{
    Idle = 1 << 0,
    Moving = 1 << 1,
    Falling = 1 << 2,
    Dashing = 1 << 3,
    LeftShooting = 1 << 4,
    RightShooting = 1 << 5,
    Zooming = 1 << 6,
    Rotating = 1 << 7,
    Spawning = 1 << 8,
    Dead = 1 << 9,

    RotateState = Moving | LeftShooting | RightShooting | Zooming,
    ActionState = Idle | Moving | Dashing | LeftShooting | RightShooting | Zooming,
    ShootState = LeftShooting | RightShooting,
    UnmanipulableState = Spawning | Dead,
}

// 파츠 부위
[Flags]
public enum EPartType
{
    ArmL = 1 << 0,
    ArmR = 1 << 1,
    Legs = 1 << 2,
    Shoulder = 1 << 3,
    Back = 1 << 4,
    Mask = 1 << 5,
}

// 파츠 종류
public enum EAttackType
{
    Basic,
    Laser,
    Rapid,
    Heavy,
}

// 파츠 연결을 위한 메시 종류
// Skinned Mesh Renderer를 사용하더라도 캐터필러처럼 움직이는 방식이 다르다면 Static을 사용
public enum EPartMeshType
{
    Skinned,
    Static
}

// 파츠 별 애니메이션
public enum EAnimationType
{
    Base = 0,
    Hover = 1,
    Roller = 2,
    Caterpillar = 3,
    ShootingBase = 4,
    ShootingHover = 5,
    ShootingRoller = 6,
    ShootingCaterpillar = 7,
}

// 파츠 별 카메라
public enum ECameraState
{
    Normal = 0,
    Hover = 1,
    Roller = 2,
    Caterpillar = 3,
    Zoom = 4,
    HoverZoom = 5,
    RollerZoom = 6,
    CaterpillarZoom = 7,
}
