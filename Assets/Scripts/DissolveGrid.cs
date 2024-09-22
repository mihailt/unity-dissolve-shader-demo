using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DissolveGrid : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cycleSpeed = 0.5f;  // Speed of the dissolve effect
    [SerializeField] private float delayBetweenTiles = 0.01f;  // Delay between each tile's dissolve

    [SerializeField] private KeyCode appearExpandKeyCode;
    [SerializeField] private KeyCode disappearExpandKeyCode;

    [SerializeField] private KeyCode appearRandomKeyCode;
    [SerializeField] private KeyCode disappearRandomKeyCode;
    
    [SerializeField] private GameObject tilePrefab;

    private readonly List<Material> _materials = new List<Material>();

    private GameObject[,] _tiles;

    private bool isAnimating;
    
    void GenerateGrid(float dissolve)
    {
        CleanupGrid();
        _tiles = new GameObject[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var tilePosition = new Vector3(transform.position.x + x, dissolve, transform.position.z + y);
                var tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
                tile.name = $"Tile ({x}, {y})";
                _tiles[x, y] = tile;
                tile.transform.SetParent(transform);

                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foreach (var mat in renderer.materials)
                    {
                        mat.SetFloat("_Dissolve", dissolve);
                    }
                }
            }
        }

        var renders = GetComponentsInChildren<Renderer>();
        foreach (var t in renders)
            _materials.AddRange(t.materials);
    }

    private void CleanupGrid()
    {
        if (_tiles != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_tiles[x, y] != null)
                    {
                        Destroy(_tiles[x, y]);
                    }
                }
            }
        }
        _tiles = null;
    }

    private void Update()
    {
        if (isAnimating)
            return;
        if (Input.GetKeyDown(appearExpandKeyCode))
        {
            StartCoroutine(AppearExpandingGrid());
        }
        else if (Input.GetKeyDown(disappearExpandKeyCode))
        {
            StartCoroutine(DisappearContractingGrid());
        }
        else if (Input.GetKeyDown(appearRandomKeyCode))
        {
            StartCoroutine(RandomAppearDisappear(true));
        }
        else if (Input.GetKeyDown(disappearRandomKeyCode))
        {
            StartCoroutine(RandomAppearDisappear(false));
        }
    }

    private float DistanceFromCenter(int x, int y)
    {
        float centerX = (width - 1) / 2f;
        float centerY = (height - 1) / 2f;
        return Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));
    }

    private List<(int x, int y, float distance)> GetSortedTileDistances()
    {
        List<(int x, int y, float distance)> tileDistances = new List<(int, int, float)>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float distance = DistanceFromCenter(x, y);
                tileDistances.Add((x, y, distance));
            }
        }
        tileDistances.Sort((a, b) => a.distance.CompareTo(b.distance));
        return tileDistances;
    }

    private IEnumerator AppearExpandingGrid()
    {
        GenerateGrid(1f);
        isAnimating = true;

        var sortedTiles = GetSortedTileDistances();

        foreach (var tile in sortedTiles)
        {
            StartCoroutine(AnimateDissolve(_tiles[tile.x, tile.y], 1f, 0f));
            yield return new WaitForSeconds(delayBetweenTiles);
        }

        isAnimating = false;
    }

    private IEnumerator DisappearContractingGrid()
    {
        if (_tiles == null)
        {
            Debug.LogWarning("Grid not generated. Call AppearExpandingGrid first.");
            yield break;
        }

        isAnimating = true;

        var sortedTiles = GetSortedTileDistances();
        sortedTiles.Reverse(); // Reverse to start from outside

        foreach (var tile in sortedTiles)
        {
            StartCoroutine(AnimateDissolve(_tiles[tile.x, tile.y], 0f, 1f));
            yield return new WaitForSeconds(delayBetweenTiles);
        }

        isAnimating = false;
    }

    private IEnumerator RandomAppearDisappear(bool isAppearing)
    {
        if (_tiles == null)
        {
            GenerateGrid(isAppearing ? 1f : 0f);
        }

        isAnimating = true;

        List<GameObject> remainingTiles = new List<GameObject>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                remainingTiles.Add(_tiles[x, y]);
            }
        }

        while (remainingTiles.Count > 0)
        {
            int index = Random.Range(0, remainingTiles.Count);
            GameObject tile = remainingTiles[index];
            remainingTiles.RemoveAt(index);

            StartCoroutine(AnimateDissolve(tile, isAppearing ? 1f : 0f, isAppearing ? 0f : 1f));
            yield return new WaitForSeconds(delayBetweenTiles);
        }

        isAnimating = false;
    }
    
    private IEnumerator AnimateDissolve(GameObject tile, float startDissolve, float endDissolve)
    {
        var renderer = tile.GetComponent<Renderer>();
        if (!renderer) yield break;

        Material material = renderer.material;
        float dissolveValue = startDissolve;
        float elapsedTime = 0f;

        while (Mathf.Abs(dissolveValue - endDissolve) > 0.01f)
        {
            dissolveValue = Mathf.Lerp(startDissolve, endDissolve, elapsedTime * cycleSpeed);
            material.SetFloat("_Dissolve", dissolveValue);
            tile.transform.position = new Vector3(tile.transform.position.x, dissolveValue, tile.transform.position.z);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        material.SetFloat("_Dissolve", endDissolve);
    }
}