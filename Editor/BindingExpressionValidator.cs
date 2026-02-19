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

        var totalErrorCount = ValidateBindingNodes();

        var errors = new List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>>();
        var allBindingListeners = ValidateBindingExpressions(errors);

        totalErrorCount += errors.Count;
        foreach (var (go, err, _) in errors)
            Debug.LogError(err.Message, go);

        if (totalErrorCount <= 0)
        {
            Debug.Log(
                $"<b>Databinding</b> : <b>Validation <color=green>OK!</color></b> for <b>{allBindingListeners.Length}</b> listeners");
        }
        else if (!EditorUtility.DisplayDialog(
            $"Databinding - {totalErrorCount} Error{(totalErrorCount > 1 ? "s" : "")} found",
            $"{totalErrorCount} {(totalErrorCount > 1 ? "errors have" : "error has")} been found in the scene!", "Play",
            "Go Back"))
        {
            EditorApplication.ExitPlaymode();
            //todo : Open the fix tab here.
        }
    }
    public static BindingListenerBase[] ValidateBindingExpressions(List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors)
    {
        var allBindingListeners = Resources.FindObjectsOfTypeAll<BindingListenerBase>();
        foreach (var bindingListener in allBindingListeners)
            bindingListener.ValidateExpressions(errors);
        return allBindingListeners;
    }
    public static int ValidateBindingNodes()
    {
        var errorCount = 0;

        var allBindingNodes = Resources.FindObjectsOfTypeAll<BindingNode>();
        foreach (var bindingNode in allBindingNodes)
            errorCount += bindingNode.ValidateErrors() ? 0 : 1;

        return errorCount;
    }
}
