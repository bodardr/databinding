using System.ComponentModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Weaver;
using Weaver.Extensions;

namespace Bodardr.Databinding.Editor
{
    public class BindableComponent : WeaverComponent
    {
        private MethodReference eventArgsConstructor;
        private MethodReference eventHandlerInvokeMethod;

        private TypeReference eventHandlerTypeRef;
        private TypeReference eventArgs;
        private TypeReference propertyChangedInterface;

        public override string ComponentName => "Bindable";

        public override DefinitionType EffectedDefintions => DefinitionType.Module | DefinitionType.Type;

        public override void VisitModule(ModuleDefinition moduleDefinition)
        {
            eventHandlerTypeRef = moduleDefinition.ImportReference(typeof(PropertyChangedEventHandler));
            propertyChangedInterface = moduleDefinition.ImportReference(typeof(INotifyPropertyChanged));
            eventArgs = moduleDefinition.ImportReference(typeof(PropertyChangedEventArgs));

            eventArgsConstructor = moduleDefinition.ImportReference(eventArgs.Resolve().GetMethod(".ctor"));
            eventHandlerInvokeMethod =
                moduleDefinition.ImportReference(eventHandlerTypeRef.Resolve().GetMethod("Invoke"));
        }

        public override void VisitType(TypeDefinition typeDefinition)
        {
            if (typeDefinition.Interfaces.All(x => x.InterfaceType.Name != propertyChangedInterface.Name))
                return;

            var customAttribute = typeDefinition.GetCustomAttribute<BindableAttribute>();

            if (customAttribute == null)
                return;

            var propertyChangedField = typeDefinition.Fields.Single(x => x.Name == "PropertyChanged");

            foreach (var property in typeDefinition.Properties)
                AddPropertyChanged(property, propertyChangedField, eventArgsConstructor, eventHandlerInvokeMethod);

            typeDefinition.CustomAttributes.Remove(customAttribute);
        }

        private void AddPropertyChanged(PropertyDefinition property, FieldDefinition propertyChangedField,
            MethodReference argsConstructor,
            MethodReference invokeMethod)
        {
            var setMethod = property.SetMethod;
            var backingField = property.DeclaringType.Fields.Single(x => x.Name == $"<{property.Name}>k__BackingField");

            setMethod.Body.Instructions.Clear();
            var ilProcessor = setMethod.Body.GetILProcessor();

            var ldarg0_000a = ilProcessor.Create(OpCodes.Ldarg_0);

            var ldarg1_00 = ilProcessor.Create(OpCodes.Ldarg_1);
            var ldarg0_01 = ilProcessor.Create(OpCodes.Ldarg_0);
            var ldfld_02 = ilProcessor.Create(OpCodes.Ldfld, backingField);
            var bneUnS_07 = ilProcessor.Create(OpCodes.Bne_Un_S, ldarg0_000a);

            var ret_09 = ilProcessor.Create(OpCodes.Ret);

            var ldarg1_000b = ilProcessor.Create(OpCodes.Ldarg_1);
            var stfld_000c = ilProcessor.Create(OpCodes.Stfld, backingField);

            var ldarg0_11 = ilProcessor.Create(OpCodes.Ldarg_0);
            var ldfld_12 = ilProcessor.Create(OpCodes.Ldfld, propertyChangedField);
            var dup_13 = ilProcessor.Create(OpCodes.Dup);
            var ldarg0_17 = ilProcessor.Create(OpCodes.Ldarg_0);
            var brtrueS_14 = ilProcessor.Create(OpCodes.Brtrue_S, ldarg0_17);
            var pop_15 = ilProcessor.Create(OpCodes.Pop);
            var ret_16 = ilProcessor.Create(OpCodes.Ret);
            var ldstr_18 = ilProcessor.Create(OpCodes.Ldstr, property.Name);
            var newobj_19 = ilProcessor.Create(OpCodes.Newobj, argsConstructor);
            var callvirt_20 = ilProcessor.Create(OpCodes.Callvirt, invokeMethod);
            var ret_21 = ilProcessor.Create(OpCodes.Ret);

            ilProcessor.Append(ldarg1_00);
            ilProcessor.Append(ldarg0_01);
            ilProcessor.Append(ldfld_02);
            ilProcessor.Append(bneUnS_07);
            ilProcessor.Append(ret_09);
            ilProcessor.Append(ldarg0_000a);
            ilProcessor.Append(ldarg1_000b);
            ilProcessor.Append(stfld_000c);
            ilProcessor.Append(ldarg0_11);
            ilProcessor.Append(ldfld_12);
            ilProcessor.Append(dup_13);
            ilProcessor.Append(brtrueS_14);
            ilProcessor.Append(pop_15);
            ilProcessor.Append(ret_16);
            ilProcessor.Append(ldarg0_17);
            ilProcessor.Append(ldstr_18);
            ilProcessor.Append(newobj_19);
            ilProcessor.Append(callvirt_20);
            ilProcessor.Append(ret_21);
        }
    }
}