using System.Windows;
using System.Windows.Controls;

/*
 *  Helper class to adjust the row size of the grid on the main window, when switching between single line 
 *  and multi line mode.
 *  I tried to adjust the height using the following trigger but it doen's work and it seems
 *  the only realiable way is to set the RowDefinitions' heigh by code.
 *  
 *  <Style TargetType = "RowDefinition" x:Key="CodeInputRowDefinitionStyle">
 *      <Setter Property = "MinHeight" Value="42" />
 *      <Setter Property = "Height" Value="auto"/>

 *      <Style.Triggers>
 *          <DataTrigger Binding = "{Binding Multiline}" Value="true">
 *              <Setter Property = "Height" Value="100"/>
 *          </DataTrigger>
 *      </Style.Triggers>
 *  </Style>
 *  
 */


namespace JupiterNet.View
{
    public class AdjustableGrid : Grid
    {
        #region InitialRowsSize

        public bool InitialRowsSize
        {
            get
            {
                return (bool)GetValue(InitialRowsSizeProperty);
            }
            set
            {
                SetValue(InitialRowsSizeProperty, value);
            }
        }

        public static readonly DependencyProperty InitialRowsSizeProperty =
            DependencyProperty.Register("InitialRowsSize", typeof(bool), typeof(AdjustableGrid),
                new PropertyMetadata(false, new PropertyChangedCallback(OnInitialRowsSizePropertyChanged)));

        private static void OnInitialRowsSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (AdjustableGrid)d;
            if ((bool)e.NewValue)
            {
                grid.RowDefinitions[1].SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
                grid.RowDefinitions[2].SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            }
            else
            {
                grid.RowDefinitions[1].SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
                grid.RowDefinitions[2].SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            }
        }

        #endregion
    }
}
