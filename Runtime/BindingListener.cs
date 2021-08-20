using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bodardr.Databinding.Runtime
{
    internal delegate object GetDelegate(object objectFrom);

    internal delegate void SetDelegate(object newValue);

    [AddComponentMenu("Databinding/Binding Listener")]
    public class BindingListener : MonoBehaviour
    {
        private GetDelegate GetMethod;
        private SetDelegate SetMethod;

        [SerializeField]
        private Component component;

        [SerializeField]
        private List<MemberInfo> memberHierarchy = new List<MemberInfo>();

        [SerializeField]
        private SearchStrategy searchStrategy;

        [SerializeField]
        private BindingBehavior bindingBehavior;

        [SerializeField]
        private string getPath = "";

        [SerializeField]
        private string setPath = "";

        public string GetPath
        {
            get => getPath;
            set
            {
                getPath = value;
                BuildGetterHierarchy();
            }
        }

        public string SetPath
        {
            get => setPath;
            set
            {
                setPath = value;
                BuildSetterHierarchy(BuildGetterHierarchy());
            }
        }

        public void Initialize()
        {
            var getterMemberType = BuildGetterHierarchy();
            BuildSetterHierarchy(getterMemberType);
        }

        private void OnValidate()
        {
            if (searchStrategy == SearchStrategy.FindInParent)
                bindingBehavior = gameObject.GetComponentInParent<BindingBehavior>();

            if (!getPath.Contains('.'))
                return;

            var type = BuildGetterHierarchy();

            if (setPath.Contains('.'))
                BuildSetterHierarchy(type);
        }

        private void Awake()
        {
            if (searchStrategy == SearchStrategy.FindInParent)
                bindingBehavior = gameObject.GetComponentInParent<BindingBehavior>();

            if (bindingBehavior == null)
            {
                Debug.LogWarning(
                    "Binding Behavior cannot be found, try changing Search Strategy or specify it manually.");
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            bindingBehavior.AddListener(GetPath, this);
        }

#if UNITY_EDITOR
        [MenuItem("CONTEXT/TextMeshProUGUI/Databinding - Add Listener")]
        public static void AddTextListener(MenuCommand menuCommand)
        {
            var bindingListener = ((Component)menuCommand.context).gameObject.AddComponent<BindingListener>();
            bindingListener.setPath = "TextMeshProUGUI.text";
        }
#endif
        private Type BuildGetterHierarchy()
        {
            var properties = getPath.Split('.');

            var param = Expression.Parameter(typeof(object));
            var cast = Expression.TypeAs(param, bindingBehavior.BoundObjectType);

            Expression expr = cast;
            for (var i = 1; i < properties.Length; i++)
                expr = Expression.PropertyOrField(expr, properties[i]);

            var compiledExpr = Expression.Lambda<GetDelegate>(expr, param);

            if (compiledExpr.CanReduce)
                compiledExpr.Reduce();

            GetMethod = compiledExpr.Compile();
            return ((MemberExpression)expr).Member.GetPropertyOrFieldType();
        }

        private void BuildSetterHierarchy(Type memberType)
        {
            var properties = setPath.Split('.');

            component = GetComponent(properties[0]);

            var targetType = component.GetType();

            //Get outward Type.
            for (var i = 1; i < properties.Length; i++)
                targetType = targetType.FindFieldsAndProperties().Single(x => x.Name == properties[i])
                    .GetPropertyOrFieldType();

            var valueParam = Expression.Parameter(typeof(object), "value");

            Expression expr = Expression.Constant(component, component.GetType());

            for (var i = 1; i < properties.Length; i++)
                expr = Expression.PropertyOrField(expr, properties[i]);

            Expression rightExpr;

            if (targetType == typeof(string))
                rightExpr = Expression.Call(valueParam, "ToString", Array.Empty<Type>());
            else
                rightExpr = Expression.TypeAs(valueParam, memberType);


            expr = Expression.Assign(expr, rightExpr);

            var compiledExpr = Expression.Lambda<SetDelegate>(expr, valueParam);

            if (compiledExpr.CanReduce)
                compiledExpr.Reduce();

            SetMethod = compiledExpr.Compile();
        }

        public void UpdateValue(object obj)
        {
            var fetchedValue = GetMethod(obj);
            SetMethod(fetchedValue);
        }

        [Serializable]
        public enum SearchStrategy
        {
            FindInParent,
            SpecifyReference
        }
    }
}