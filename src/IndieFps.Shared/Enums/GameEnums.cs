namespace IndieFps.Shared.Enums;

public enum SubscriptionTier
{
    Free = 0,
    Pro = 1
}

public enum SubscriptionState
{
    Unpaid = 0,
    Trial = 1,
    Active = 2,
    PastDue = 3,
    Cancelled = 4,
    Expired = 5
}

public enum Entitlement
{
    LevelsTutorial = 0,
    LevelsAll = 1,
    Multiplayer = 2,
    Cosmetics = 3,
    ModSupport = 4,
    CloudSaves = 5
}

public enum AuthProvider
{
    Email = 0,
    Steam = 1,
    Google = 2,
    Apple = 3
}

public enum Platform
{
    Windows = 0,
    MacOS = 1,
    Linux = 2
}