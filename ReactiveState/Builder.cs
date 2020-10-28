using System;
using System.Linq.Expressions;

namespace ReactiveState
{
	public static class Builder
	{
		public static ICloneBuilder<T> Clone<T>() => new CloneBuilder<T>(new ConstructorCloneBuilder<T>());

		public static Func<T, T> Compile<T>(this ICloneBuilder<T> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, T> Compile<T, P1>(this ICloneBuilder<T, P1> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, T> Compile<T, P1, P2>(this ICloneBuilder<T, P1, P2> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, T> Compile<T, P1, P2, P3>(this ICloneBuilder<T, P1, P2, P3> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, T> Compile<T, P1, P2, P3, P4>(this ICloneBuilder<T, P1, P2, P3, P4> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, P5, T> Compile<T, P1, P2, P3, P4, P5>(this ICloneBuilder<T, P1, P2, P3, P4, P5> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, P5, P6, T> Compile<T, P1, P2, P3, P4, P5, P6>(this ICloneBuilder<T, P1, P2, P3, P4, P5, P6> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, P5, P6, P7, T> Compile<T, P1, P2, P3, P4, P5, P6, P7>(this ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, P5, P6, P7, P8, T> Compile<T, P1, P2, P3, P4, P5, P6, P7, P8>(this ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, T> Compile<T, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9> cloneBuilder) => cloneBuilder.Build().Compile();
		public static Func<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T> Compile<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>(this ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10> cloneBuilder) => cloneBuilder.Build().Compile();

		public static ConstructorReducerBuilder<TState, TAction> Reducer<TState, TAction>()
			where TAction : IAction
			=> new ConstructorReducerBuilder<TState, TAction>();

		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, TValue>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, default(TValue));
	}
}
