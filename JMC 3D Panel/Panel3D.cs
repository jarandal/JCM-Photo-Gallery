using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using DrWPF.Windows.Controls;
using System.ComponentModel;

namespace WpfDiscipleBlogViewer3D
{
    /// <summary>
    /// A Panel that displays its children in a 
    /// Viewport3D hosted in the adorner layer.
    /// </summary>
    public class Panel3D : LogicalPanel
    {
        #region Data

        static readonly Point ORIGIN_POINT = new Point(0, 0);

        readonly DependencyPropertyDescriptor _isSelectedPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(ListBoxItem.IsSelectedProperty, typeof(ListBoxItem));
        Viewport3D _viewport;
        readonly Dictionary<DependencyObject, Viewport2DVisual3D> _visualTo3DModelMap = new Dictionary<DependencyObject, Viewport2DVisual3D>();

        #endregion // Data

        // Scene.xaml is used in here.
        #region Constructor

        public Panel3D()
        {
            _viewport = Application.LoadComponent(
                new Uri(@"JMC 3D Panel\Scene.xaml", UriKind.Relative))
                as Viewport3D;
        }

        #endregion // Constructor

        #region MoveItems

        /// <summary>
        /// Moves the items forward or backward one position over one second.
        /// </summary>
        public void MoveItems(int itemCount, bool forward)
        {
            this.MoveItems(itemCount, forward, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Moves the items forward or backward one position over the specified length of time.
        /// </summary>
        public void MoveItems(int itemCount, bool forward, TimeSpan animationLength)
        {
            if (itemCount < 0 || _viewport.Children.Count - 1 < itemCount)
                throw new ArgumentOutOfRangeException("itemCount");

            // We cannot move items less than two items.
            // The first item is a light source, so ignore it.
            if (_viewport.Children.Count < 3)
                return;

            #region Create Lists

            // Get a list of all the Viewport2DVisual3D and TranslateTransform3D objects in the viewport.
            List<Viewport2DVisual3D> viewport2Ds = new List<Viewport2DVisual3D>();
            List<TranslateTransform3D> transforms = new List<TranslateTransform3D>();
            foreach (object model in _viewport.Children)
            {
                if (model is ModelVisual3D)
                    continue;

                var viewport2D = model as Viewport2DVisual3D;
                viewport2Ds.Add(viewport2D);
                transforms.Add(viewport2D.Transform as TranslateTransform3D);
            }

            #endregion // Create Lists

            #region Relocate Target Item

            for (int i = 0; i < itemCount; ++i)
            {
                // Move the first or last item to the opposite end of the list.
                if (forward)
                {
                    var firstGeo = viewport2Ds[0];
                    viewport2Ds.RemoveAt(0);
                    viewport2Ds.Add(firstGeo);

                    // The item at index 0 holds the scene's light
                    // so don't remove that, instead remove the first
                    // model that we added to the scene in code.
                    var firstChild = _viewport.Children[1];
                    _viewport.Children.RemoveAt(1);
                    _viewport.Children.Add(firstChild);
                }
                else
                {
                    int idx = viewport2Ds.Count - 1;
                    var lastGeo = viewport2Ds[idx];
                    viewport2Ds.RemoveAt(idx);
                    viewport2Ds.Insert(0, lastGeo);

                    idx = _viewport.Children.Count - 1;
                    var lastChild = _viewport.Children[idx];
                    _viewport.Children.RemoveAt(idx);
                    _viewport.Children.Insert(1, lastChild);
                }
            }

            #endregion // Relocate Target Item

            #region Select Front Item

            var frontVisual = _viewport.Children[1] as Viewport2DVisual3D;
            var listBoxItem = frontVisual.Visual as ListBoxItem;
            if (listBoxItem != null)
            {
                var listbox = ListBox.GetItemsOwner(this) as ListBox;
                listbox.SelectedItem = listBoxItem.DataContext;
            }

            #endregion // Select Front Item

            //****************************************************************************
            // Added Code To Make Later Items IsHitTestVisible = false
            // Not Sure About Light Source _viewport.Children[0], but I am not changing it
            ((_viewport.Children[1] as Viewport2DVisual3D).Visual as ListBoxItem).IsHitTestVisible = true;
            for (int i = 2; i < _viewport.Children.Count; i++)
                ((_viewport.Children[i] as Viewport2DVisual3D).Visual as ListBoxItem).IsHitTestVisible = false;
            //****************************************************************************

            #region Animate All Items to New Locations and Opacitys

            // Apply the new transforms via animations.
            for (int i = 0; i < transforms.Count; ++i)
            {
                double targetX = (i + 1) * -1;
                double targetY = (i + 1) * +1;
                double targetZ = (i + 1) * -8;

                var trans = viewport2Ds[i].Transform as TranslateTransform3D;

                Duration duration = new Duration(animationLength);

                DoubleAnimation animX = new DoubleAnimation();
                animX.To = targetX;
                animX.Duration = duration;
                animX.AccelerationRatio = forward ? 0 : 1;
                animX.DecelerationRatio = forward ? 1 : 0;
                trans.BeginAnimation(TranslateTransform3D.OffsetXProperty, animX);

                DoubleAnimation animY = new DoubleAnimation();
                animY.To = targetY;
                animY.AccelerationRatio = forward ? 0.7 : 0.3;
                animY.DecelerationRatio = forward ? 0.3 : 0.7;
                animY.Duration = duration;
                trans.BeginAnimation(TranslateTransform3D.OffsetYProperty, animY);

                DoubleAnimation animZ = new DoubleAnimation();
                animZ.To = targetZ;
                animZ.AccelerationRatio = forward ? 0.3 : 0.7;
                animZ.DecelerationRatio = forward ? 0.7 : 0.3;
                animZ.Duration = duration;
                trans.BeginAnimation(TranslateTransform3D.OffsetZProperty, animZ);

                DoubleAnimation animOpacity = new DoubleAnimation();
                animOpacity.To = 1 / (i + 1.0);
                animOpacity.AccelerationRatio = 0.2;
                animOpacity.DecelerationRatio = 0.8;
                animOpacity.Duration = duration;
                var elem = viewport2Ds[i].Visual as FrameworkElement;
                elem.BeginAnimation(FrameworkElement.OpacityProperty, animOpacity);
            }

            #endregion // Animate All Items to New Locations and Opacitys
        }

        #endregion // MoveItems

        #region Layout Overrides

        protected override Size ArrangeOverride(Size finalSize)
        {
            _viewport.Arrange(new Rect(ORIGIN_POINT, finalSize));
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // make sure the viewport is parented on first measure
            if (_viewport.Tag == null)
            {
                AddVisualChild(_viewport);
                _viewport.Tag = string.Empty;
            }
            _viewport.Measure(availableSize);

            return _viewport.DesiredSize;
        }

        #endregion  // Layout Overrides

        #region Create 3D Objects

        protected override void OnLogicalChildrenChanged(UIElement visualAdded, UIElement visualRemoved)
        {
            // Do not create a model for the Viewport3D.
            if (visualAdded == _viewport) 
                return;

            bool add = visualAdded != null && !_visualTo3DModelMap.ContainsKey(visualAdded);
            if (add)
            {
                var model = BuildInteractive3DModel(visualAdded as FrameworkElement);
                _visualTo3DModelMap.Add(visualAdded, model);
                _viewport.Children.Add(model);

                if (visualAdded is ListBoxItem)
                    _isSelectedPropertyDescriptor.AddValueChanged(visualAdded, this.OnListBoxItemSelected);
            }

            bool remove = visualRemoved != null && _visualTo3DModelMap.ContainsKey(visualRemoved);
            if (remove)
            {
                var model = _visualTo3DModelMap[visualRemoved];                
                _visualTo3DModelMap.Remove(visualRemoved);
                _viewport.Children.Remove(model);

                if (visualAdded is ListBoxItem)
                    _isSelectedPropertyDescriptor.RemoveValueChanged(visualAdded, this.OnListBoxItemSelected);
            }
        }

        void OnListBoxItemSelected(object sender, EventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null || !listBoxItem.IsSelected)
                return;

            var model = _visualTo3DModelMap[listBoxItem];

            // The first item in the Viewport3D's Children is the
            // light source, so ignore it.
            int idx = _viewport.Children.IndexOf(model) - 1;

            // Bring the selected item to the front.
            this.MoveItems(idx, true);
        }

        Viewport2DVisual3D BuildInteractive3DModel(FrameworkElement cp)
        {
            cp.Opacity = 1.0 / Math.Max(_viewport.Children.Count, 1);

            var model = new Viewport2DVisual3D
            {
                Geometry = new MeshGeometry3D
                {
                    TriangleIndices = new Int32Collection(
                        new int[] { 0, 1, 2, 2, 3, 0 }),
                    TextureCoordinates = new PointCollection(
                        new Point[] 
                            { 
                                new Point(0, 1), 
                                new Point(1, 1), 
                                new Point(1, 0), 
                                new Point(0, 0) 
                            }),
                    Positions = new Point3DCollection(
                        new Point3D[] 
                            { 
                                new Point3D(-1, -1, 0), 
                                new Point3D(+1, -1, 0), 
                                new Point3D(+1, +1, 0), 
                                new Point3D(-1, +1, 0) 
                            })
                },
                Material = new DiffuseMaterial
                {
                    Brush = Brushes.Transparent
                },
                Transform = new TranslateTransform3D
                {
                    OffsetX = _viewport.Children.Count * -1,
                    OffsetY = _viewport.Children.Count * +1,
                    OffsetZ = _viewport.Children.Count * -8
                },
                // Host ContentPresenter in the 3D object.
                Visual = cp
            };

            Viewport2DVisual3D.SetIsVisualHostMaterial(
                model.Material, true);
            return model;
        }

        #endregion // Create 3D Objects
    }
}