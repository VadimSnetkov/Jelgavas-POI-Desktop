# Jelgavas POI Desktop

A Windows desktop application for exploring points of interest in **Jelgava, Latvia**, built with **C# and WPF**. Browse local places, find them on an interactive map, and test your knowledge of the city in the **POI Hunt** mini-game.

## Features

- **41 built-in locations** across six categories: electronics, food, bars and clubs, parks, shops, and attractions.
- **List and map views** for browsing places.
- **Place details** including descriptions, addresses, ratings, opening hours, and photos.
- **Search** by place name or category.
- **Filters** for category, ratings of 4+, locations within 1 km of the reference point, and places listed as open 24/7.
- **Sorting** by category, rating, or distance.
- **Interactive map** with markers, zooming, panning, and optional distance lines.
- **POI Hunt mini-game** with difficulty levels, timed challenges, scoring, and a speed mode with power-ups.

## Technologies

- C#
- .NET 6 (`net6.0-windows`)
- Windows Presentation Foundation (WPF)
- XAML
- LINQ

Place information is stored directly in C# collections. No database or API key is required.

## Getting Started

### Requirements

- Windows x64
- .NET 6 SDK
- Visual Studio with the **.NET desktop development** workload, or the .NET CLI
- Internet access to load externally hosted place photos

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/VadimSnetkov/Jelgavas-POI-Desktop.git
   cd Jelgavas-POI-Desktop
   ```

2. Open `Project.sln` in Visual Studio.

3. Update the map image path in `Project/Map.xaml`.

   The current `Image.Source` uses an absolute path from the original development machine. Replace it with the full path to `Project/pics/jelgava_map.jpg` on your computer.

4. Restore NuGet packages, build the solution, and run it with **F5**.

Alternatively, after updating the image path, run from the repository root:

```bash
dotnet restore
dotnet run --project Project/Project.csproj
```

## Usage

### Explore Places

- Select a place in the list to view its details.
- Use the search field and category dropdown to narrow the results.
- Enable rating, distance, or 24/7 filters as needed.
- Click the sorting button to cycle through category, rating, and distance.
- Switch to the map to explore locations visually.

### Map Controls

| Action | Control |
| --- | --- |
| Zoom in or out | Mouse wheel |
| Pan the map | Hold the middle mouse button and drag |
| Reveal a place name | Hover over its marker |
| Open place details | Click the place label |
| Display distance lines | Enable **Show Distance Lines** |
| Return to the list | Click **List view** |

### POI Hunt

Click **POI Hunt** in the map view and find the requested locations before time runs out.

| Difficulty | Time per location | Wrong selection |
| --- | --- | --- |
| Easy | 15 seconds | No penalty |
| Normal | 10 seconds | −1 point |
| Hard | 5 seconds | −2 points |
| Expert | 5 seconds | Game over |

Speed mode adds power-ups such as double points, time freeze, hints, and skipping a location.

## Project Structure

| File | Purpose |
| --- | --- |
| `Project.sln` | Visual Studio solution |
| `Project/MainWindow.xaml` | List view and place details interface |
| `Project/MainWindow.xaml.cs` | List navigation, filtering, and sorting |
| `Project/Map.xaml` | Map interface |
| `Project/Map.xaml.cs` | Map interactions and POI Hunt logic |
| `Project/Places.cs` | Place model |
| `Project/PlacesData.cs` | Built-in locations and opening hours |
| `Project/PlaceService.cs` | Search, sorting, and distance calculations |
| `Project/pics/` | Map image assets |

## Notes

- Place information and ratings are static and do not update automatically.
- Descriptions and category names are primarily in Latvian.
- Distances are approximate straight-line estimates calculated from map coordinates, using Jelgava Palace as the reference point. They are not walking or driving routes.
- Place photos depend on external image URLs.
- The map image path must be configured before using the map on another computer.
