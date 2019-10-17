using System.Windows;

namespace JupiterNet.View
{
    public class ListUtils
    {
        #region IsEditing

        public static bool GetIsEditing(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEditingProperty);
        }

        public static void SetIsEditing(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEditingProperty, value);
        }

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.RegisterAttached("IsEditing", typeof(bool), typeof(ListUtils), new PropertyMetadata(false));

        #endregion
    }
}
