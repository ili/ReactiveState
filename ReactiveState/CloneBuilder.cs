using System;
using System.Linq.Expressions;

namespace ReactiveState
{
	public interface ICloneBuilder<T>
	{
		Expression<Func<T, T>> Build();
		ICloneBuilder<T, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1>
	{
		Expression<Func<T, P1, T>> Build();
		ICloneBuilder<T, P1, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2>
	{
		Expression<Func<T, P1, P2, T>> Build();
		ICloneBuilder<T, P1, P2, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3>
	{
		Expression<Func<T, P1, P2, P3, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4>
	{
		Expression<Func<T, P1, P2, P3, P4, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P4, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4, P5>
	{
		Expression<Func<T, P1, P2, P3, P4, P5, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P4, P5, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4, P5, P6>
	{
		Expression<Func<T, P1, P2, P3, P4, P5, P6, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7>
	{
		Expression<Func<T, P1, P2, P3, P4, P5, P6, P7, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8>
	{
		Expression<Func<T, P1, P2, P3, P4, P5, P6, P7, P8, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9>
	{
		Expression<Func<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, T>> Build();
		ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P> Add<P>(Expression<Func<T, P>> expression);
	}

	public interface ICloneBuilder<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>
	{
		Expression<Func<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T>> Build();
		ConstructorCloneBuilder<T> Add<P>(Expression<Func<T, P>> expression);
	}


	class CloneBuilder<T> : ICloneBuilder<T>
	{
		private readonly ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			_builder = builder;
		}

		public ICloneBuilder<T, P> Add<P>(Expression<Func<T, P>> expression)
		{
			_builder.Add(expression);
			return new CloneBuilder<T, P>(_builder);
		}

		public Expression<Func<T, T>> Build()
			=> (Expression<Func<T, T>>)_builder.Build();
	}

	class CloneBuilder<T, P> : ICloneBuilder<T, P>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			_builder = builder;
		}

		public ICloneBuilder<T, P, P1> Add<P1>(Expression<Func<T, P1>> expression)
			=> new CloneBuilder<T, P, P1>(_builder.Add(expression));

		public Expression<Func<T, P, T>> Build()
		=> (Expression<Func<T, P, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1> : ICloneBuilder<T, P, P1>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			_builder = builder;
		}

		public ICloneBuilder<T, P, P1, P2> Add<P2>(Expression<Func<T, P2>> expression)
			=> new CloneBuilder<T, P, P1, P2>(_builder.Add(expression));

		public Expression<Func<T, P, P1, T>> Build()
			=> (Expression<Func<T, P, P1, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2> : ICloneBuilder<T, P, P1, P2>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			this._builder = builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3> Add<P3>(Expression<Func<T, P3>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, T>> Build()
			=> (Expression<Func<T, P, P1, P2, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3> : ICloneBuilder<T, P, P1, P2, P3>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			this._builder = builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3, P4> Add<P4>(Expression<Func<T, P4>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3, P4>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, P3, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3, P4> : ICloneBuilder<T, P, P1, P2, P3, P4>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> _builder)
		{
			this._builder = _builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3, P4, P5> Add<P5>(Expression<Func<T, P5>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3, P4, P5>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, P3, P4, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, P4, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3, P4, P5> : ICloneBuilder<T, P, P1, P2, P3, P4, P5>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> _builder)
		{
			this._builder = _builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6> Add<P6>(Expression<Func<T, P6>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3, P4, P5, P6>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, P3, P4, P5, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, P4, P5, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3, P4, P5, P6> : ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			this._builder = builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7> Add<P7>(Expression<Func<T, P7>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, P3, P4, P5, P6, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, P4, P5, P6, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7> : ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			this._builder = builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8> Add<P8>(Expression<Func<T, P8>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, P3, P4, P5, P6, P7, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, P4, P5, P6, P7, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8> : ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			this._builder = builder;
		}

		public ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8, P9> Add<P9>(Expression<Func<T, P9>> expression)
			=> new CloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8, P9>(_builder.Add(expression));

		public Expression<Func<T, P, P1, P2, P3, P4, P5, P6, P7, P8, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, P4, P5, P6, P7, P8, T>>)_builder.Build();
	}

	class CloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8, P9> : ICloneBuilder<T, P, P1, P2, P3, P4, P5, P6, P7, P8, P9>
	{
		private ConstructorCloneBuilder<T> _builder;

		public CloneBuilder(ConstructorCloneBuilder<T> builder)
		{
			this._builder = builder;
		}

		public ConstructorCloneBuilder<T> Add<P10>(Expression<Func<T, P10>> expression)
			=> _builder.Add(expression);

		public Expression<Func<T, P, P1, P2, P3, P4, P5, P6, P7, P8, P9, T>> Build()
			=> (Expression<Func<T, P, P1, P2, P3, P4, P5, P6, P7, P8, P9, T>>)_builder.Build();
	}
}
