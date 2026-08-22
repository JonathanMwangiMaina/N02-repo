using Godot;
using IndieFps.Client.Subscription;

namespace IndieFps.Client.Subscription;

[GlobalClass]
public partial class EntitlementGate : Node
{
    [Export] public string RequiredEntitlement { get; set; } = "";
    [Export] public NodePath TargetNodePath { get; set; }
    [Export] public bool DisableInsteadOfHide { get; set; } = false;
    [Export] public bool RequireProTier { get; set; } = false;
    
    private SubscriptionManager _subscriptionManager = null!;
    private Node _targetNode = null!;
    
    public override void _Ready()
    {
        _subscriptionManager = GetNode<SubscriptionManager>("/root/SubscriptionManager");
        _targetNode = GetNodeOrNull(TargetNodePath) ?? this;
        
        _subscriptionManager.OnStatusChanged += OnStatusChanged;
        _subscriptionManager.OnEntitlementChanged += OnEntitlementChanged;
        
        ApplyGate(_subscriptionManager.CachedStatus);
    }
    
    private void OnStatusChanged(IndieFps.Shared.DTOs.SubscriptionStatusDto status)
    {
        ApplyGate(status);
    }
    
    private void OnEntitlementChanged(bool hasProAccess)
    {
        ApplyGate(_subscriptionManager.CachedStatus);
    }
    
    private void ApplyGate(IndieFps.Shared.DTOs.SubscriptionStatusDto? status)
    {
        bool hasAccess = false;
        
        if (RequireProTier)
        {
            hasAccess = status?.Tier == IndieFps.Shared.Enums.SubscriptionTier.Pro 
                     && status.State is IndieFps.Shared.Enums.SubscriptionState.Active or IndieFps.Shared.Enums.SubscriptionState.Trial;
        }
        else if (!string.IsNullOrEmpty(RequiredEntitlement))
        {
            hasAccess = status?.Entitlements.Contains(RequiredEntitlement) == true;
        }
        
        if (DisableInsteadOfHide)
        {
            _targetNode.Set("disabled", !hasAccess);
        }
        else
        {
            _targetNode.Visible = hasAccess;
        }
    }
    
    public override void _ExitTree()
    {
        if (_subscriptionManager != null)
        {
            _subscriptionManager.OnStatusChanged -= OnStatusChanged;
            _subscriptionManager.OnEntitlementChanged -= OnEntitlementChanged;
        }
        base._ExitTree();
    }
}