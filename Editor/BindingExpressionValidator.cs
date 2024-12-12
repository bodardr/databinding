using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime;
using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public class BindingExpressionValidator
{
    static BindingExpressionValidator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange obj)
    {
        if (obj != PlayModeStateChange.ExitingEditMode)
            return;

        var allBindingNodes = Resources.FindObjectsOfTypeAll<BindingNode>();
        foreach (var bindingNode in allBindingNodes)
            bindingNode.ValidateErrors();
        
        var allBindingListeners = Resources.FindObjectsOfTypeAll<BindingListenerBase>();

        var errors = new List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>>();
        foreach (var bindingListener in allBindingListeners)
            bindingListener.ValidateExpressions(errors);

        foreach (var (go, err, _) in errors)
            Debug.LogError(err.Message, go);

        if (errors.Count <= 0)
        {
            Debug.Log(
                $"<b>Databinding</b> : <b>Validation <color=green>OK!</color></b> for <b>{allBindingListeners.Length}</b> listeners");
        }
        else if (!EditorUtility.DisplayDialog($"Databinding - {errors.Count} Error{(errors.Count > 1 ? "s" : "")} found",
            $"{errors.Count} {(errors.Count > 1 ? "errors have" : "error has")} been found in the scene!", "Play",
            "Go Back"))
        {
            EditorApplication.ExitPlaymode();


            //todo : Open the fix tab here.
        }
    }
}
