﻿#pragma checksum "C:\Users\Jarno\Desktop\WindowsGameCenter\WindowsGameCenter\WindowsGameCenter\GameCenter.Helpers\GameDetailsControl.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "DE534607D810F438A521040E43F5E5B47BC1591EB22E30562B030CE22A6E55CA"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GameCenter.Helpers
{
    partial class GameDetailsControl : 
        global::Microsoft.UI.Xaml.Controls.UserControl, 
        global::Microsoft.UI.Xaml.Markup.IComponentConnector
    {

        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 3.0.0.2502")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 2: // GameDetailsControl.xaml line 11
                {
                    this.ExpandStoryboard = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Media.Animation.Storyboard>(target);
                }
                break;
            case 3: // GameDetailsControl.xaml line 22
                {
                    this.CollapseStoryboard = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Media.Animation.Storyboard>(target);
                }
                break;
            case 4: // GameDetailsControl.xaml line 35
                {
                    this.RootGrid = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.Grid>(target);
                    ((global::Microsoft.UI.Xaml.Controls.Grid)this.RootGrid).PointerEntered += this.RootGrid_PointerEntered;
                    ((global::Microsoft.UI.Xaml.Controls.Grid)this.RootGrid).PointerExited += this.RootGrid_PointerExited;
                }
                break;
            case 5: // GameDetailsControl.xaml line 45
                {
                    this.CardTransform = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Media.ScaleTransform>(target);
                }
                break;
            case 6: // GameDetailsControl.xaml line 69
                {
                    this.ExpandedContent = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.Grid>(target);
                }
                break;
            case 7: // GameDetailsControl.xaml line 77
                {
                    this.GameDescription = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.TextBlock>(target);
                }
                break;
            case 8: // GameDetailsControl.xaml line 96
                {
                    this.DLCListView = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.ListView>(target);
                }
                break;
            case 9: // GameDetailsControl.xaml line 88
                {
                    this.ScreenshotsPanel = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.StackPanel>(target);
                }
                break;
            case 10: // GameDetailsControl.xaml line 60
                {
                    this.GameImage = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.Image>(target);
                }
                break;
            case 11: // GameDetailsControl.xaml line 63
                {
                    this.GameTitle = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.TextBlock>(target);
                }
                break;
            case 12: // GameDetailsControl.xaml line 64
                {
                    this.GameLauncher = global::WinRT.CastExtensions.As<global::Microsoft.UI.Xaml.Controls.TextBlock>(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }


        /// <summary>
        /// GetBindingConnector(int connectionId, object target)
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 3.0.0.2502")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Microsoft.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Microsoft.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

