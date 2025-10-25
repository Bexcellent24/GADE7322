using UnityEngine;
using System.Collections.Generic;

public interface IUpgradable
{
    int CurrentUpgradeLevel { get; }
    int MaxUpgradeLevel { get; }
    bool CanUpgrade();
    int GetUpgradeCost();
    void ApplyUpgrade();
    UpgradeType GetUpgradeType();
}
