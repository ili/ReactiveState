using System;
using System.Collections.Generic;
using System.Linq;
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

		/// <summary>
		/// Reserts property to default(TValue)
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <typeparam name="TAction"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="builder">builder</param>
		/// <param name="memberExpression">member expression for member to reset</param>
		/// <returns></returns>
		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, TValue>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, default(TValue));

		/// <summary>
		/// Resets property to <see cref="Enumerable.Empty{TResult}"/>
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <typeparam name="TAction"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="builder">builder</param>
		/// <param name="memberExpression">member expression for member to reset</param>
		/// <returns></returns>
		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, IEnumerable<TValue>>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, Enumerable.Empty<TValue>());

		/// <summary>
		/// Resets property to <see cref="Array.Empty{T}"/>
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <typeparam name="TAction"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="builder">builder</param>
		/// <param name="memberExpression">member expression for member to reset</param>
		/// <returns></returns>
		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, IReadOnlyCollection<TValue>>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, Array.Empty<TValue>());

		/// <summary>
		/// Resets property to <see cref="Array.Empty{T}"/>
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <typeparam name="TAction"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="builder">builder</param>
		/// <param name="memberExpression">member expression for member to reset</param>
		/// <returns></returns>
		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, IList<TValue>>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, Array.Empty<TValue>());

		/// <summary>
		/// Resets property to <see cref="Array.Empty{T}"/>
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <typeparam name="TAction"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="builder">builder</param>
		/// <param name="memberExpression">member expression for member to reset</param>
		/// <returns></returns>
		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, ICollection<TValue>>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, Array.Empty<TValue>());

		/// <summary>
		/// Resets property to <see cref="Array.Empty{T}"/>
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <typeparam name="TAction"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="builder">builder</param>
		/// <param name="memberExpression">member expression for member to reset</param>
		/// <returns></returns>
		public static ConstructorReducerBuilder<TState, TAction> Reset<TState, TAction, TValue>(this ConstructorReducerBuilder<TState, TAction> builder, Expression<Func<TState, IReadOnlyList<TValue>>> memberExpression)
			where TAction : IAction
			=> builder.Add(memberExpression, Array.Empty<TValue>());

	}
}
