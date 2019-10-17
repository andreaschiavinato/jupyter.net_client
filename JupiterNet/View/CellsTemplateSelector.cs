using JupiterNet.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace JupiterNet.View
{
    public class CellsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate InputCellTemplate { get; set; }
        public DataTemplate TextCellTemplate { get; set; }
        public DataTemplate ImageCellTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case NotebookVM.InputCellVM _:
                    return InputCellTemplate;

                case NotebookVM.TextCellVM _:
                    return TextCellTemplate;

                case NotebookVM.ImageCellVM _:
                    return ImageCellTemplate;

                default:
                    throw new Exception("Invalid cell type");
                    
            }
        }

    }
}
