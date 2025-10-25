using UnityEngine;
using System.Collections.Generic;

public interface IVisualUpgradeStrategy
{
    void ApplyVisualUpgrade(int upgradeLevel, UpgradeConfiguration config);
    bool IsValid();
}