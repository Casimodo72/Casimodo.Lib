// Copyright (c) 2009 Kasimier Buchcik
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyView = System.Windows.Data.ListCollectionView;

namespace Casimodo.Lib.Presentation
{
    /// <summary>
    /// Wraps each data item automatically into the specified item-ViewModel.
    /// </summary>
    // TODO: I think I'll drop this one here in the future. It becomes too messy.
    //[Obsolete("Don't use this one. The mess of automatically combining the data's view model with the data is not worth the brain acrobatics.")]
    public class CollectionViewModel<TData, TModel> : CollectionViewModel<TData>
        where TModel : ItemViewModel<TData>, new()
    {

        ObservableCollection<TModel> _models;
        TModel _previousCurrentModel;

        public CollectionViewModel()
        { }

        public sealed override IList ViewItems
        {
            get { return _models; }
        }

        public bool MoveCurrentTo(TModel model)
        {
            return _view.MoveCurrentTo(model);
        }

        public TModel CurrentModel
        {
            get { return (TModel)_view.CurrentItem; }
        }

        protected sealed override MyView CreateView()
        {
            _models = new ObservableCollection<TModel>();
            return new MyView(_models);
        }

        public sealed override void Clear(bool refreshView = false)
        {
            CheckNotDisposed();

            if (_view.Count == 0 && _effectiveItems.Count == 0)
                return;

            _view.MoveCurrentToPosition(-1);
            _effectiveItems.Clear();
            _models.Clear();

            _changed = true;

            if (refreshView)
                _view.Refresh();
        }

        protected sealed override void OnViewCurrentChanged(object sender, EventArgs e)
        {
            CheckNotDisposed();

            if (_previousCurrentModel == _view.CurrentItem)
            {
                // Yes, we do want to be *only* notified when the current item *really* changes.
                return;
            }

            if (_previousCurrentModel != null)
            {
                // Deselect the previous current ViewModel.
                _previousCurrentModel.IsSelected = false;
                _previousCurrentModel = null;
            }

            TModel curModel = (TModel)_view.CurrentItem;

            if (curModel != null)
            {
                // Select the current ViewModel.
                curModel.IsSelected = true;
                _previousCurrentModel = curModel;
            }

            UpdateCommands();
            DeselectAllCommand.RaiseCanExecuteChanged();

            // Binding ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            TData data = (curModel != null) ? curModel.Data : default(TData);

            UpdateToSource(data);

            RaiseCurrentItemChanged();
        }

        public bool IsChangeTrackingChanged
        {
            get { return _changed; }
        }

        public void MakeChangeTrackingSnapshot()
        {
            _changed = false;
        }

        bool _changed;

        public override bool Remove(TData data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            TModel model = (TModel)GetViewItemOfData(data);
            if (model == null)
                return false;

            bool result = false;
            _isRemoving = true;
            try
            {
                result = _effectiveItems.Remove(data);
                if (result)
                {
                    _models.Remove(model);
                    _changed = true;
                }
            }
            finally
            {
                _isRemoving = false;
            }
            return result;
        }

        public void Add(TData data, TModel model)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (model == null)
                throw new ArgumentNullException("model");

            model.Data = data;
            _effectiveItems.Add(data);
            _models.Add(model);
            _changed = true;
        }

        protected sealed override void AddToView(TData data)
        {
            // Wrap the data in its ViewModel.
            TModel model = new TModel();
            model.Data = data;

            _models.Add(model);
        }

        protected sealed override void InsertToView(int index, TData data)
        {
            // Wrap the data in its ViewModel.
            TModel model = new TModel();
            model.Data = data;

            _models.Insert(index, model);
        }

        protected sealed override TData GetCurrentItem()
        {
            TModel model = (TModel)_view.CurrentItem;
            if (model != null)
                return model.Data;
            return default(TData);
        }

        protected sealed override object GetViewItemOfData(TData data)
        {
            return _models.FirstOrDefault(x => object.Equals(data, x.Data));
        }

        public TModel FindModel(TData data)
        {
            return _models.FirstOrDefault(x => object.Equals(data, x.Data));
        }

        public TModel ModelOf(TData data)
        {
            var model = _models.FirstOrDefault(x => object.Equals(data, x.Data));
            if (model == null)
                throw new Exception("No view model exists for the given data in the CollectionViewModel.");

            return model;
        }

        /// <summary>
        /// Note that this returns the models in the view; i.e. the current filter has an effect on the
        /// set of returned models.
        /// </summary>
        public IEnumerable<TModel> Models
        {
            get
            {
                IEnumerator enumerator = ((IEnumerable)_view).GetEnumerator();
                TModel cur;
                while (enumerator.MoveNext())
                {
                    cur = enumerator.Current as TModel;

                    // We must not return the CollectionView.NewItemPlaceholder.
                    if (cur != null)
                        yield return cur;
                }

                yield break;
            }
        }

        /// <summary>
        /// Returns the model in the view at the given position.
        /// </summary>        
        public TModel ModelAt(int position)
        {
            return Models.ElementAt(position);
        }

        public sealed override IEnumerator<TData> GetEnumerator()
        {
            IEnumerator enumerator = ((IEnumerable)_view).GetEnumerator();
            TModel cur;
            while (enumerator.MoveNext())
            {
                cur = enumerator.Current as TModel;

                // We must not return the CollectionView.NewItemPlaceholder.
                if (cur != null)
                    yield return cur.Data;
            }

            yield break;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            _models = null;
        }
    }
}
