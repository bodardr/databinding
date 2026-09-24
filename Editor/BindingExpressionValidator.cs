using System;
using System.Collections.Generic;
using Bodardr.Databinding.Runtime;
using Mono.Cecil;
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

        var expressionErrors = new List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>>();
        var totalErrorCount = ValidateBindingNodes(expressionErrors, out _);
        ValidateBindingExpressions(expressionErrors, out var allBindingListeners);

        totalErrorCount += expressionErrors.Count;
        foreach (var (go, err, _) in expressionErrors)
            Debug.LogError(err.Message, go);

        if (totalErrorCount <= 0)
        {
            Debug.Log($"<b>Databinding</b> : <b>Validation <color=green>OK!</color></b> for <b>{allBindingListeners.Length}</b> listeners");
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
    
    public static void ValidateBindingExpressions(List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors, out BindingListenerBase[] allBindingListeners)
    {
        allBindingListeners = Resources.FindObjectsOfTypeAll<BindingListenerBase>();
        foreach (var bindingListener in allBindingListeners)
            bindingListener.ValidateExpressions(errors);
    }
    
    public static int ValidateBindingNodes(List<Tuple<GameObject, BindingExpressionErrorContext, IBindingExpression>> errors, out BindingNode[] allBindingNodes)
    {
        var errorCount = 0;

        allBindingNodes = Resources.FindObjectsOfTypeAll<BindingNode>();
        foreach (var bindingNode in allBindingNodes)
            errorCount += bindingNode.ValidateErrors(errors) ? 0 : 1;

        return errorCount;
    }
}
