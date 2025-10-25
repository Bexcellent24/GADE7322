using UnityEngine;
using System.Collections.Generic;


public interface ISelectable
{
    void OnSelected();
    void OnDeselected();
    bool IsSelectable { get; }
}