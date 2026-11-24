using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
  [Header("Map Settings")]
  public int width = 10;
  public int height = 10;
  public GameObject tilePrefab;
  public string valueMap;
  public const int MAX_MOVE_COST = 5;

  [Header("Materials")]
  public Material lightMaterial;
  public Material darkMaterial;
  public Gradient terrainColors;

  private Tile[,] map;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake()
  {
    map = new Tile[width, height];
    GenerateGrid();
  }

  public void GenerateGrid()
  {
    for (int x = 0; x < width; x++)
    {
      for (int y = 0; y < width; y++)
      {
        // puts grid manager in the center of the map vector(x,y,z)
        Vector3 tilePosition = new Vector3(x - width/2, 0, y - height/2);
        GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
        tile.name = $"Tile {x},{y}";
        // makes every tile a child of the GridManager
        tile.transform.SetParent(transform);

        Renderer renderer = tile.GetComponentInChildren<Renderer>();

        renderer.material = new Material((x + y) % 2 == 0 ? lightMaterial : darkMaterial);

        Tile tileScript = tile.GetComponent<Tile>();
        tileScript.gridPosition = new Vector2Int(x, y);
        tileScript.moveCost = valueMap[x + y * width] - '0';
        float normalizedCost = (float)tileScript.moveCost / MAX_MOVE_COST;
        tileScript.originalColor =  tileScript.moveCost > MAX_MOVE_COST
                                      ? Color.red
                                      : terrainColors.Evaluate(normalizedCost);
        map[x, y] = tileScript;
      }
    }
  }

  private List<Tile> GetTileNeighbors(Vector2Int tilePosition)
  {
    List<Tile> neighbors = new List<Tile>();

    for(int x = -1; x <= 1; x++)
    {
      for(int y = -1; y <= 1; y++)
      {
        int posX = tilePosition.x + x;
        int posY = tilePosition.y + y;

        if (
          posX >= 0 &&
          posY >= 0 &&
          posX < width &&
          posY < height &&
          new Vector2Int(posX, posY) != tilePosition)
        {
          neighbors.Add(map[posX, posY]);
        }
      }
    }

    return neighbors;
  }

  private bool IsDiagonal(Tile a, Tile b)
{
    int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
    int dy = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
    return dx == 1 && dy == 1;
  }

  private void ResetGridHighlights()
  {
    foreach(Tile tile in map)
    {
      if(Tile.selectedTile != tile)
      {
        tile.inMoveRange = false;
        tile.ChangeColor(tile.originalColor);
      }
    }
  }

  public List<Tile> GetHighlightRange(Vector2Int start, int range)
  {
    ResetGridHighlights();

    List<Tile> reachable            = new List<Tile>();
    Dictionary<Tile, int> costSoFar = new Dictionary<Tile, int>();
    Queue<Tile> edge                = new Queue<Tile>();

    Tile startTile = map[start.x, start.y];
    edge.Enqueue(startTile);
    costSoFar[startTile] = 0;

    while(edge.Count > 0)
    {
      Tile current = edge.Dequeue();
      int currentCost = costSoFar[current];

      foreach(Tile neighbor in GetTileNeighbors(current.gridPosition))
      {
        // int stepCost = IsDiagonal(current, neighbor) ? 1 + neighbor.moveCost : neighbor.moveCost;
        int stepCost = neighbor.moveCost + (IsDiagonal(current, neighbor) ? 1 : 0);
        int newCost  = currentCost + stepCost;

        if(
          newCost <= range &&
          (
            !costSoFar.ContainsKey(neighbor) ||
            newCost < costSoFar[neighbor]
          ) &&
          !neighbor.isOccupied)
        {
          costSoFar[neighbor] = newCost;
          edge.Enqueue(neighbor);

          if(!reachable.Contains(neighbor) && neighbor != startTile)
          {
            reachable.Add(neighbor);
            neighbor.inMoveRange = true;
          }
        }
      }
    }
    return reachable;
  }

  public void HighlightMoveRange(Tile start, int range)
  {
    List<Tile> reachableTiles = GetHighlightRange(start.gridPosition, range);

    foreach(Tile tile in reachableTiles)
    {
      tile.ChangeColor(Color.cyan);
    }
  }

  public Tile GetTile(Vector2Int position)
  {
    return map[position.x, position.y];
  }

  public Tile GetTile(Vector3 position)
  {
    float offsetX = width / 2f;
    float offsetY = height / 2f;

    int x = Mathf.RoundToInt(position.x + offsetX);
    int y = Mathf.RoundToInt(position.z + offsetY);

    return map[x, y];
  }

  private int GetHeuristic (Tile start, Tile end)
  {
    return Mathf.Abs(start.gridPosition.x - end.gridPosition.x) + Mathf.Abs(start.gridPosition.y - end.gridPosition.y);
  }

  private List<Tile> RetracePath(Tile start, Tile end)
  {
    List<Tile> path = new List<Tile>();
    Tile current = end;

    while(current != start)
    {
      path.Add(current);
      current = current.parent;
    }

    path.Reverse();
    return path;
  }

  public List<Tile> GetPath(Tile start, Tile end)
  {
    List<Tile> openSet = new List<Tile>();
    HashSet<Tile> closedSet = new HashSet<Tile>();

    openSet.Add(start);
    start.gCost = 0;
    start.hCost = GetHeuristic(start, end);

    while(openSet.Count > 0)
    {
      Tile current = openSet.OrderBy(tile => tile.fCost).ThenBy(tile => tile.hCost).First();

      if (current == end)
      {
        return RetracePath(start, end);
      }

      openSet.Remove(current);
      closedSet.Add(current);

      foreach(Tile neighbor in GetTileNeighbors(current.gridPosition))
      {
        if (neighbor.isOccupied || closedSet.Contains(neighbor)) continue;

        int newGCost = current.gCost + neighbor.moveCost;

        if(newGCost < neighbor.gCost || !openSet.Contains(neighbor))
        {
          neighbor.gCost = newGCost;
          neighbor.hCost = GetHeuristic(neighbor, end);
          neighbor.parent = current;

          if(!openSet.Contains(neighbor))
          {
            openSet.Add(neighbor);
          }
        }
      }
    }

    return null;
  }
}
