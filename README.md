[![NuGet](http://img.shields.io/nuget/vpre/Sm.Maui.BottomSheet.svg?label=NuGet)](https://www.nuget.org/packages/Sm.Maui.BottomSheet) [![GitHub issues](https://img.shields.io/github/issues/S1LV3Rman/BottomSheet?style=flat-square)](https://github.com/S1LV3Rman/BottomSheet) [![GitHub stars](https://img.shields.io/github/stars/S1LV3Rman/BottomSheet?style=flat-square)](https://github.com/S1LV3Rman/BottomSheet/stargazers) ![last commit](https://img.shields.io/github/last-commit/S1LV3Rman/BottomSheet?style=flat-square)

# BottomSheet

ModalBottomSheet for .NET MAUI

## Installation
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [Sm.Maui.BottomSheet](https://www.nuget.org/packages/Sm.Maui.BottomSheet/) from the package manager console:
````bash
PM> Install-Package Sm.Maui.BottomSheet 
````

## Usage - Implementation
In order to use this BottomSheet view, you must wrap page content into Grid and add there ModalBottomSheet

```xaml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:bottomSheet="clr-namespace:S1LV3Rman.BottomSheet;assembly=BottomSheet"
             x:Class="BottomSheetDemo.MainPage">

    <Grid>
    
        <!-- Your page content -->
    
        <bottomSheet:ModalBottomSheet>
        
            <!-- BottomSheet content -->
        
        </bottomSheet:ModalBottomSheet>
        
    </Grid>
    
</ContentPage>
```

## Usage - Interaction
To open use `ShowAsync()`, to close use `HideAsync()` or you can toggle state with `ToggleAsync()`

```xaml
<Grid>

    <Button Text="Show"
            VerticalOptions="Start"
            Clicked="ShowButtonClicked"/>

    <bottomSheet:ModalBottomSheet x:Name="bottomSheet">

            <Button Text="Hide"
                    Clicked="HideButtonClicked"/>

    </bottomSheet:ModalBottomSheet>
    
</Grid>
```

```csharp
private void ShowButtonClicked(object sender, EventArgs e)
{
  bottomSheet.ShowAsync();
}

private void HideButtonClicked(object sender, EventArgs e)
{
  bottomSheet.HideAsync();
}
```

## Usage - Available properties

### `CornersRadius`
Sets top left and right corners radius

```xaml
<bottomSheet:ModalBottomSheet CornersRadius="25">
</bottomSheet:ModalBottomSheet>
```

### `Color`
Sets the color

```xaml
<bottomSheet:ModalBottomSheet Color="White">
</bottomSheet:ModalBottomSheet>
```

### `BlockExpanding`
Disables expanding to fullscreen

```xaml
<bottomSheet:ModalBottomSheet BlockExpanding="True">
</bottomSheet:ModalBottomSheet>
```

### `IsOpened`
Readonly. `True` when opened, `False when closed
