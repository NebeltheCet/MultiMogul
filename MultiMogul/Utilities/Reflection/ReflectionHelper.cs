#nullable enable
using MultiMogul.Utilities;
using System;
using System.Collections.Concurrent;
using System.Reflection;

#region MemberKey
internal readonly struct MemberKey(Type type, string name, BindingFlags flags) : IEquatable<MemberKey> {
	public readonly Type Type = type;
	public readonly string Name = name;
	public readonly BindingFlags Flags = flags;

	public bool Equals(MemberKey other) {
		return this.Type == other.Type && this.Name == other.Name && this.Flags == other.Flags;
	}

	public override bool Equals(object? obj) {
		return obj is MemberKey other && this.Equals(other);
	}

	public override int GetHashCode() {
		unchecked {
			int hash = 17;
			hash = (hash * 23) + this.Type.GetHashCode();
			hash = (hash * 23) + this.Name.GetHashCode();
			hash = (hash * 23) + this.Flags.GetHashCode();

			return hash;
		}
	}
}
#endregion

#region Reflection Caches
internal static class ReflectionCache {
	internal static readonly ConcurrentDictionary<MemberKey, FieldInfo> Fields = new();
	internal static readonly ConcurrentDictionary<MemberKey, PropertyInfo> Properties = new();
	internal static readonly ConcurrentDictionary<MemberKey, MethodInfo> Methods = new();
}
#endregion

#region Reflection Wrappers
public sealed class ReflectionField<T> {
	private readonly FieldInfo _fieldInfo;
	private readonly object _instance;

	internal ReflectionField(object instance, FieldInfo fieldInfo) {
		this._instance = instance ?? throw new ArgumentNullException(nameof(instance));
		this._fieldInfo = fieldInfo ?? throw new ArgumentNullException(nameof(fieldInfo));
	}

	public T Value {
		get => (T)this._fieldInfo.GetValue(this._instance);
		set => this._fieldInfo.SetValue(this._instance, value);
	}

	public static implicit operator T(ReflectionField<T> field) => field.Value;
}

public sealed class ReflectionProperty<T> {
	private readonly PropertyInfo _propertyInfo;
	private readonly object _instance;

	internal ReflectionProperty(object instance, PropertyInfo propertyInfo) {
		this._instance = instance ?? throw new ArgumentNullException(nameof(instance));
		this._propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
	}

	public T Value {
		get => (T)this._propertyInfo.GetValue(this._instance);
		set => this._propertyInfo.SetValue(this._instance, value);
	}

	public static implicit operator T(ReflectionProperty<T> prop) => prop.Value;
}

public sealed class ReflectionMethod<TResult> {
	private readonly MethodInfo _methodInfo;
	private readonly object _instance;

	internal ReflectionMethod(object instance, MethodInfo methodInfo) {
		this._instance = instance ?? throw new ArgumentNullException(nameof(instance));
		this._methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
	}

	public TResult Invoke() => (TResult)this._methodInfo.Invoke(this._instance, null);

	public static implicit operator Func<TResult>(ReflectionMethod<TResult> m)
		=> m.Invoke;
}
#endregion

#region ReflectionHelper
public static class ReflectionHelper {
	public static ReflectionField<T> GetField<T>(object instance, string name, BindingFlags flags) {
		var type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));
		var key = new MemberKey(type, name, flags);

		var cachedInfo = ReflectionCache.Fields.GetOrAdd(key, k =>
		{
			var fieldInfo = k.Type.GetField(k.Name, k.Flags)
				?? throw new MissingFieldException(k.Type.Name, k.Name);

			MMLog.Log($"cached reflection field[{k.Type.Name}.{k.Name}]", LogTypes.ControlFlow);
			return fieldInfo;
		});

		return new ReflectionField<T>(instance, cachedInfo);
	}

	public static ReflectionProperty<T> GetProperty<T>(object instance, string name, BindingFlags flags) {
		var type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));
		var key = new MemberKey(type, name, flags);

		var cachedInfo = ReflectionCache.Properties.GetOrAdd(key, k =>
		{
			var propertyInfo = k.Type.GetProperty(k.Name, k.Flags)
				?? throw new MissingMemberException(k.Type.Name, k.Name);

			MMLog.Log($"cached reflection property[{k.Type.Name}.{k.Name}]", LogTypes.ControlFlow);
			return propertyInfo;
		});

		return new ReflectionProperty<T>(instance, cachedInfo);
	}

	public static ReflectionMethod<T> GetMethod<T>(object instance, string name, BindingFlags flags) {
		var type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));
		var key = new MemberKey(type, name, flags);

		var cachedInfo = ReflectionCache.Methods.GetOrAdd(key, k =>
		{
			var methodInfo = k.Type.GetMethod(k.Name, k.Flags)
				?? throw new MissingMethodException(k.Type.Name, k.Name);

			MMLog.Log($"cached reflection method[{k.Type.Name}.{k.Name}]", LogTypes.ControlFlow);
			return methodInfo;
		});


		return new ReflectionMethod<T>(instance, cachedInfo);
	}
}
#endregion