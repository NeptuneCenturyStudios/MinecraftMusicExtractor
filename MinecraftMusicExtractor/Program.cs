using System.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// See https://aka.ms/new-console-template for more information
// Set how many menu items there are
const int NUM_MENU_ITEMS = 2;
// Currently selected menu option.
var _selectionIndex = 0;
// Stores the asset name and hash as strings
var _objectHash = new Dictionary<string, string>();

Console.WriteLine("Minecraft Music Extractor");
// Create a menu state object to track user's selection
var menuState = new MenuState()
{
    ExtractMusic = true
};

// Prompt user for the minecraft folder
string? _minecraftDir = null;
while (_minecraftDir == null)
{
    Console.Write("Enter location of .minecraft folder. (Example: home/.minecraft): ");
    _minecraftDir = Console.ReadLine();
}

// Once we have the minecraft directory, look into the assets folder and find the indexes. This keeps a
// maooing of resource names and hashes. We need to look up the resource name and locate the file by
// its hash.
// TODO: Look up asset indexes.

// Once we have the indexes let user decide what files to extract.
// Present the menu.
PrintMainMenu(menuState);

// Start main loop.
ConsoleKeyInfo _key = default;
while (_key.Key != ConsoleKey.Escape)
{
    _key = Console.ReadKey(true);

    // If key is down arrow, increase selection
    if (_key.Key == ConsoleKey.DownArrow)
    {
        MoveSelectionDown();
    }
    else if (_key.Key == ConsoleKey.UpArrow)
    {
        MoveSelectionUp();
    }
    else if (_key.Key == ConsoleKey.Spacebar)
    {
        SetSelection(menuState);

    }
    else if (_key.Key == ConsoleKey.Enter)
    {
        // Validate and proceed.
        await LoadIndexes();
        break;
    }

    // Move back to the top of the menu
    Console.CursorTop -= NUM_MENU_ITEMS;

    // Print the menu again
    PrintMainMenu(menuState);
}

//
void PrintMainMenu(MenuState menuState)
{
    // Print each menu option and update with selection
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write($"{GetSelectionCursor(0)} ");
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"[{GetSelectionCharacter(menuState.ExtractMusic)}] Extract music");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write($"{GetSelectionCursor(1)} ");
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"[{GetSelectionCharacter(menuState.ExtractSounds)}] Extract sounds");
    Console.WriteLine();

}

void SetSelection(MenuState menuState)
{
    switch (_selectionIndex)
    {
        case 0:
            menuState.ExtractMusic = !menuState.ExtractMusic;
            break;
        case 1:
            menuState.ExtractSounds = !menuState.ExtractSounds;
            break;
    }
}

static string GetSelectionCharacter(bool value)
{
    // Return an x if true or space if false
    return value ? "x" : " ";
}

string GetSelectionCursor(int menuIndex)
{
    return _selectionIndex == menuIndex ? ">" : " ";
}

void MoveSelectionUp()
{
    // Increase selection index
    _selectionIndex--;
    if (_selectionIndex < 0)
    {
        // Go to the end
        _selectionIndex = NUM_MENU_ITEMS - 1;
    }
}

void MoveSelectionDown()
{
    // Increase selection index
    _selectionIndex++;
    if (_selectionIndex > NUM_MENU_ITEMS - 1)
    {
        // Go back to beginning
        _selectionIndex = 0;
    }
}

async Task LoadIndexes()
{
    Console.WriteLine("\nChecking indexes...");

    var assetsFolder = Path.Combine(_minecraftDir, "assets");
    var indexesFolder = Path.Combine(assetsFolder, "indexes");
    if (Directory.Exists(indexesFolder))
    {
        // Look up the .json files in the folder.
        var indexFiles = Directory.EnumerateFiles(indexesFolder, "*.json");
        Console.WriteLine("Reading indexes...");
        foreach (var indexFile in indexFiles)
        {
            var file = new FileInfo(indexFile);
            // Read the index and deserialize the json file.
            var indexJson = await File.ReadAllTextAsync(indexFile);
            // Deserialize to a JObject
            var objectIndex = (JObject?)JsonConvert.DeserializeObject(indexJson);
            if (objectIndex != null)
            {
                // Add the indexes to a dictionary
                var assets = objectIndex["objects"]?.Children();
                
                if (assets != null)
                {
                    var percentComplete = 100 / assets.AsJEnumerable().Count();
                    foreach (var asset in assets)
                    {
                        var assetName = asset.Value<JProperty>()?.Name;
                        if (assetName != null && !_objectHash.ContainsKey(assetName))
                        {
                            _objectHash.Add(assetName, "");
                            Console.Write($"Loading assets from {file.FullName}. {percentComplete * 100}%");
                            // Set cursor back to beginning
                            Console.CursorLeft = 0;
                        }
                        
                    }
                }


            }
        }
    }
}

