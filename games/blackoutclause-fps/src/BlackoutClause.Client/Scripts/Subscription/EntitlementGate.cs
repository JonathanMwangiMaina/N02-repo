using BlackoutClause.Client.Subscription;
using Godot;

namespace BlackoutClause.Client.Subscription;

/// <summary>
/// Node that controls visibility/enabled state of a target node based on subscription entitlements.
/// Attach to any node and configure the required entitlement or Pro tier requirement.
/// </summary>
[GlobalClass]
public partial class EntitlementGate : Node
{
    /// <summary>
    /// The entitlement identifier required for access (e.g., "multiplayer", "cosmetics").
    /// </summary>
    [Export]
    public string RequiredEntitlement { get; set; } = "";

    /// <summary>
    /// Path to the target node to control. If null, controls this node.
    /// </summary>
    [Export]
    public NodePath? TargetNodePath { get; set; }

    /// <summary>
    /// If true, disables the target node instead of hiding it.
    /// </summary>
    [Export]
    public bool DisableInsteadOfHide { get; set; } = false;

    /// <summary>
    /// If true, requires Pro tier subscription (active or trial) for access.
    /// </summary>
    [Export]
    public bool RequireProTier { get; set; } = false;

    private SubscriptionManager _subscriptionManager = null!;
    private Node _targetNode = null!;

    /// <inheritdoc/>
    public override void _Ready()
    {
        _subscriptionManager = GetNode<SubscriptionManager>("/root/SubscriptionManager");
        _targetNode = GetNodeOrNull(TargetNodePath) ?? this;

        _subscriptionManager.OnStatusChanged += OnStatusChanged;
        _subscriptionManager.OnEntitlementChanged += OnEntitlementChanged;

        ApplyGate(_subscriptionManager.CachedStatus);
    }

    private void OnStatusChanged(BlackoutClause.Shared.DTOs.SubscriptionStatusDto status)
    {
        ApplyGate(status);
    }

    private void OnEntitlementChanged(bool hasProAccess)
    {
        ApplyGate(_subscriptionManager.CachedStatus);
    }

    private void ApplyGate(BlackoutClause.Shared.DTOs.SubscriptionStatusDto? status)
    {
        bool hasAccess = false;

        if (RequireProTier)
        {
            hasAccess = status?.Tier == BlackoutClause.Shared.Enums.SubscriptionTier.Pro
                     && status.State is BlackoutClause.Shared.Enums.SubscriptionState.Active or BlackoutClause.Shared.Enums.SubscriptionState.Trial;
        }
        else if (!string.IsNullOrEmpty(RequiredEntitlement))
        {
            hasAccess = status?.Entitlements.Contains(RequiredEntitlement) == true;
        }

        if (DisableInsteadOfHide)
        {
            _targetNode.Set("disabled", !hasAccess);
        }
        else if (_targetNode is CanvasItem canvasItem)
        {
            canvasItem.Visible = hasAccess;
        }
        else
        {
            // For non-CanvasItem nodes, use process mode
            _targetNode.ProcessMode = hasAccess ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        }
    }

    /// <inheritdoc/>
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
