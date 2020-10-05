using Mup.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mup.Helpers
{
    /// <summary> Defines two methods that should be performed around a scope. </summary>
    public interface IScopableOperation
    {
        #region Methods

        void PreOperation();
        void PostOperation();

        #endregion
    }

    /// <summary> A wrapper of <see cref="Action"/> instances that should be performed around a scope. </summary>
    public class ScopableOperation : IScopableOperation
    {
        #region Constructors

        public ScopableOperation(Action preAction, Action postAction)
        {
            this.PreAction = preAction;
            this.PostAction = postAction;
        }

        #endregion

        #region Properties

        protected Action PreAction { get; }
        protected Action PostAction { get; }

        #endregion

        #region IScoped Implementation

        public void PreOperation() => this.PreAction?.Invoke();
        public void PostOperation() => this.PostAction?.Invoke();

        #endregion
    }

    /// <summary> Provides a mechanism for performing operations before and after a <see langword="using"/> scope. </summary>
    public class Scope : IDisposable
    {
        #region Constructors

        /// <summary> Initializes a new instance with actions to perform before and after a <see langword="using"/> scope. </summary>
        public Scope(Action preOperation, Action postOperation)
            : this(new ScopableOperation(preOperation, postOperation))
        {
        }

        /// <summary> Initializes a new instance with an arbitrary number of operations to perform around a <see langword="using"/> scope. </summary>
        public Scope(params IScopableOperation[] scopes)
        {
            this.Operations = new List<IScopableOperation>();
            try
            {
                scopes.Each(scope => scope.With(this.Operations.Add).PreOperation());
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        #endregion

        #region Properties

        // cant be List<> because List has a void method called Reverse :(
        protected IList<IScopableOperation> Operations { get; }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this.Operations.Reverse().Each(scope => scope.PostOperation());
        }

        #endregion
    }

    /// <summary> Provides a mechanism for performing operations before and after a <see langword="using"/> scope while tracking a generic value. </summary>
    public class Scope<T> : Scope
    {
        #region Constructors

        /// <summary> Initializes a new instance with a value to track and actions to perform before and after a <see langword="using"/> scope. </summary>
        public Scope(T value, Action preOperation, Action postOperation)
            : this(value, new ScopableOperation(preOperation, postOperation))
        {
        }

        /// <summary> Initializes a new instance with a value to track and an arbitrary number of operations to perform around a <see langword="using"/> scope. </summary>
        public Scope(T value, params IScopableOperation[] scopes)
            : base(scopes)
        {
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary> The value tracked by the scope. </summary>
        public T Value { get; }

        #endregion
    }
}