using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ReactiveState
{
	public interface IStoreBuilder
	{
		IStoreBuilder Use(Func<Dispatcher, Dispatcher> middleware);
		Dispatcher Build();
		IServiceProvider Services { get; }
	}

	public class StoreBuilder : IStoreBuilder
	{
		private readonly List<Func<Dispatcher, Dispatcher>> _middlewares = new List<Func<Dispatcher, Dispatcher>>();

		public StoreBuilder(IServiceProvider services)
		{
			Services = services;
		}

		public IServiceProvider Services { get; }

		public Dispatcher Build()
		{
			Dispatcher pipe = action => Task.CompletedTask;

			for (var i = 0; i < _middlewares.Count; i++)
				pipe = _middlewares[i](pipe);

			return pipe;
		}

		public IStoreBuilder Use(Func<Dispatcher, Dispatcher> middleware)
		{
			_middlewares.Add(middleware);
			return this;
		}
	}

	public class DispatcherBuilder
	{
		private readonly List<Func<Dispatcher, Dispatcher>> _middlewares = new List<Func<Dispatcher, Dispatcher>>();

		public DispatcherBuilder Use(params Func<Dispatcher, Dispatcher>[] middlewares)
		{
			_middlewares.AddRange(middlewares);
			return this;
		}

		public Dispatcher Build()
		{
			Dispatcher pipe = action => Task.CompletedTask;

			for (var i = 0; i < _middlewares.Count; i++)
				pipe = _middlewares[i](pipe);

			return pipe;
		}

	}

	public static class UseExtensions
	{
		/// <summary>
		/// Uses <paramref name="dispatcher"/> as dispatcher and continues pipe
		/// </summary>
		/// <param name="storeBuilder"></param>
		/// <param name="dispatcher"></param>
		public static IStoreBuilder Use(this IStoreBuilder storeBuilder, Dispatcher dispatcher)
			=> storeBuilder.Use(next => a => { dispatcher(a); return next(a); });

		/// <summary>
		/// Uses <paramref name="action"/> as dispatcher and continues pipe
		/// </summary>
		/// <param name="storeBuilder"></param>
		/// <param name="action"></param>
		public static IStoreBuilder Use(this IStoreBuilder storeBuilder, Action<IAction> action)
			=> storeBuilder.Use(a => { action(a); return Task.CompletedTask; });
	}


	public class Store : IStore
	{
		private readonly Dispatcher _dispatcher;

		public Store(Dispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public Task Dispatch(IAction action)
			=> _dispatcher(action);

		public IObservable<IStateAccessor> States()
		{
			throw new NotImplementedException();
		}
	}

	public static class BuildExtensions
	{
		public static IStoreBuilder StoreBuilder(this IServiceProvider serviceProvider)
			=> new StoreBuilder(serviceProvider);

		public static IServiceCollection Add<TState, TAction>(this IServiceCollection serviceCollection, Reducer<TState, TAction> reducer)
			where TAction: IAction
		{
			serviceCollection.AddSingleton<IReducer>(new ReducerImpl<TState, TAction>(reducer));
			return serviceCollection;
		}
	}
}
