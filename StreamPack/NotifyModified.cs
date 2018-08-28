using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamPack
{
    /// <summary>
    /// 
    /// </summary>
    public class ModificationEventArgs
    {
        public object Source { get; private set; }
        public object InnerSource { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ModificationEventArgs() : this(null, null)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public ModificationEventArgs(object source) : this(source, null)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public ModificationEventArgs(object source, object innerSource)
        {
            this.Source = source;
            this.InnerSource = innerSource;
        }
    }

    /// <summary>
    /// Interface of an object which notifies about its being modified.
    /// </summary>
    public interface INotifyModified
    {
        event Action<ModificationEventArgs> Modified;
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class NotifyModifiedBase : INotifyModified
    {
        private event Action<ModificationEventArgs> _modified;
        /// <summary>
        /// The event when the property's value is changed to another reference or value.
        /// </summary>
        public event Action<ModificationEventArgs> Modified
        {
            add { this._modified += value; }
            remove { this._modified -= value; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void RaiseModifiedEvent()
        {
            if (_modified != null)
                _modified.Invoke(new ModificationEventArgs(this));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerSource"></param>
        protected void RaiseModifiedEvent(object innerSource)
        {
            if (_modified != null)
                _modified.Invoke(new ModificationEventArgs(this, innerSource));
        }
    }
}
