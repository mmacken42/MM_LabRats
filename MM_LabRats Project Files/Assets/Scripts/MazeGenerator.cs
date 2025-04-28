using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MazeGenerator : MonoBehaviour
{
    //Game controller ref
    private MazeGameController gameController;

    //Maze details
    [Tooltip("cell size should be 1x1 with walls 0.1 thick")]
    public MazeCell prefabMazeCell;
    [Range(5, 30)]
    public int numRowsInMaze = 10;
    [Range(5, 30)]
    public int numColumnsInMaze = 10;
    
    private Vector2Int mazeSize;
    private readonly float offsetBtwCells = 0.9f; //cells are 1x1, walls are 0.1 thick, so 0.9 offsets
    //Maze is represented by a List of MazeCells
    private List<MazeCell> cells;
    //Also need to keep track of current path through the maze, as well as completed paths (to avoid revisiting)
    List<MazeCell> currentPath;
    List<MazeCell> completedCells;

    //Animation options (just looks cool)
    public bool animateMazeGeneration = true;
    [Range(0.01f, 0.09f)]
    public float delaySecondsBtwAnimatedMazeCellOperations = 0.025f;

    //Camera ref (for repositioning based on maze size)
    [Tooltip("Camera reconfigures based on chosen maze size")]
    public Camera gameCamera;

    //Game UI refs
    public GameObject textGenNewMaze;
    public GameObject textPlayerControls;

    //Player
    public PlayerLabRat prefabPlayer;
    private PlayerLabRat playerRef;
    private float secondsBeforeSpawningPlayer = 1f;

    //Cheese Goal (ends level when collided with)
    public Cheese prefabCheese;
    private Cheese cheeseRef;

    //maze gen helpers
    private bool buildingNewMaze;
    private float cameraRotationSpeed = 15f;

    //Player ability cooldown progress bars
    private CheeseSniffingCooldownBar myCooldownForSniffing;
    private PoopCooldownBar myCooldownForPooping;

    private void Start()
    {
        //GenerateNewMaze();

        gameController = GetComponent<MazeGameController>();
        myCooldownForSniffing = gameController.myCooldownForSniffing;
        myCooldownForPooping = gameController.myCooldownForPooping;
    }

    private void Update()
    {
        if (buildingNewMaze)
        {
            gameCamera.transform.Rotate(Vector3.forward * cameraRotationSpeed * Time.deltaTime);
        }
    }

    public void GenerateNewMaze()
    {   
        myCooldownForSniffing.ToggleBarVisibile(false);
        myCooldownForPooping.ToggleBarVisibile(false);
        
        buildingNewMaze = false;
        
        DestroyOldPlayer();
        //turn off camera that watches maze being built
        gameCamera.enabled = true;

        textGenNewMaze.SetActive(true);

        DestroyOldMaze();

        mazeSize = new Vector2Int(numRowsInMaze, numColumnsInMaze);

        SetupGameCamera();

        if (animateMazeGeneration)
        {
            StartCoroutine(GenerateMazeAnimated_DFS(mazeSize));
        }
        else
        {
            GenerateMazeInstant_DFS(mazeSize);
        }
    }

    public void DestroyOldMaze()
    {
        foreach (Transform childT in gameObject.transform)
        {
            GameObject.Destroy(childT.gameObject);
        }

        GameObject.Destroy(cheeseRef);
    }

    public void DestroyOldPlayer()
    {
        if (playerRef != null)
        {
            playerRef.DeletePoops();

            GameObject.Destroy(playerRef);
        }

        textPlayerControls.SetActive(false);
    }

    private void SetupGameCamera()
    {
        Vector3 cameraPos = Vector3.zero;

        switch(mazeSize.x)
        {
            case 6:
                cameraPos = new Vector3(2f, 5f, 2.25f);
                break;
            case 12:
                cameraPos = new Vector3(4.5f, 9f, 4.5f);
                break;
            case 18:
                cameraPos = new Vector3(7.5f, 14f, 8f);
                break;
            case 24:
                cameraPos = new Vector3(10f, 20f, 10f);
                break;
            case 30:
                cameraPos = new Vector3(12.5f, 24f, 12.5f);
                break;
        }

        gameCamera.transform.SetPositionAndRotation(cameraPos, Quaternion.Euler(90, 0, 0));

        buildingNewMaze = true;
    }

    /*General approach is to use a randomized Depth-first Search, a.k.a. "recursive backtracking"
     1. create a grid of maze cells, with walls on all four sides of each cell
     2. randomly choose a starting cell
     3. randomly choose a direction to "move" to an unvisited cell (right, left, up, or down)
     4. knock down the walls in the direction we chose to "move" go to the previously unvisited cell
     5. add this new cell to a List of visited cells (essentially acting as a Stack, LIFO), to support backtracking
     6. when we reach a cell that has no unvisited neighbors, this is a "dead-end"
     7. on hitting a dead-end, backtrack until we reach a cell that still has unvisited neighbors
     8. repeat steps 3-7 until we have visited every single cell (we end up back at starting cell from step 2)
    Note: it is possible to do this recursively, however it is more performant to break it out into a loop.*/
    private IEnumerator GenerateMazeAnimated_DFS(Vector2Int size)
    {
        cells = new List<MazeCell>();

        //create a grid of cells (walls on all four sides of each cell created)
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                CreateNewMazeCellAtPosition(x, y);

                yield return null;
            }
        }

        InitPathTracking();

        //keep going as long as there are more neighboring cells to visit that haven't been visited before
        while (completedCells.Count < cells.Count)
        {
            FindUnvisitedNeighborCellOrBacktrackIfNone(size);

            //brief delay in between operations so we can see the maze generation in real-time
            yield return new WaitForSeconds(delaySecondsBtwAnimatedMazeCellOperations);
        }

        SetupStartAndEndOfMaze();

        yield return new WaitForSeconds(secondsBeforeSpawningPlayer);

        buildingNewMaze = false;
        SpawnPlayerRat();
    }

    //Same as GenerateMazeAnimated_DFS except without any delays or "animations"
    private void GenerateMazeInstant_DFS(Vector2Int size)
    {
        cells = new List<MazeCell>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                CreateNewMazeCellAtPosition(x, y);
            }
        }

        InitPathTracking();

        while (completedCells.Count < cells.Count)
        {
            FindUnvisitedNeighborCellOrBacktrackIfNone(size);
        }

        SetupStartAndEndOfMaze();

        buildingNewMaze = false;
        SpawnPlayerRat();
    }

    private void CreateNewMazeCellAtPosition(int x, int y)
    {
        Vector3 nextPos = new Vector3(x * offsetBtwCells, 0, y * offsetBtwCells);
        MazeCell newMazeCell = Instantiate(prefabMazeCell, nextPos, Quaternion.identity, transform);
        newMazeCell.SetState(MazeCellGenerationState.Untouched);
        cells.Add(newMazeCell);
    }

    private void InitPathTracking()
    {
        //keep track of current path through the maze, as well as completed paths (to avoid dupe visits)
        currentPath = new List<MazeCell>();
        completedCells = new List<MazeCell>();

        //randomly choose a cell to start with
        currentPath.Add(cells[UnityEngine.Random.Range(0, cells.Count)]); //currentPath acting as a Stack (Last in, first out)
        //mark its state as Current (this also changes color of cell)
        currentPath[0].SetState(MazeCellGenerationState.Current);
    }

    private void FindUnvisitedNeighborCellOrBacktrackIfNone(Vector2Int size)
    {
        // Check cells next to the current cell
        List<int> possibleNextCells = new List<int>();
        List<int> possibleDirections = new List<int>();

        int currentCellIndex = cells.IndexOf(currentPath[currentPath.Count - 1]); //"Pop" top element of our "Stack" (LIFO)
        int currentCellX = currentCellIndex / size.y;
        int currentCellY = currentCellIndex % size.y;

        //RIGHT
        if (currentCellX < size.x - 1)
        {
            if (!completedCells.Contains(cells[currentCellIndex + size.y]) && 
                !currentPath.Contains(cells[currentCellIndex + size.y]))
            {
                possibleDirections.Add(1);
                possibleNextCells.Add(currentCellIndex + size.y);
            }
        }
        //LEFT
        if (currentCellX > 0)
        {
            if (!completedCells.Contains(cells[currentCellIndex - size.y]) &&
                !currentPath.Contains(cells[currentCellIndex - size.y]))
            {
                possibleDirections.Add(2);
                possibleNextCells.Add(currentCellIndex - size.y);
            }
        }
        //UP
        if (currentCellY < size.y - 1)
        {
            if (!completedCells.Contains(cells[currentCellIndex + 1]) &&
                !currentPath.Contains(cells[currentCellIndex + 1]))
            {
                possibleDirections.Add(3);
                possibleNextCells.Add(currentCellIndex + 1);
            }
        }
        //DOWN
        if (currentCellY > 0)
        {
            if (!completedCells.Contains(cells[currentCellIndex - 1]) &&
                !currentPath.Contains(cells[currentCellIndex - 1]))
            {
                possibleDirections.Add(4);
                possibleNextCells.Add(currentCellIndex - 1);
            }
        }

        //randomly choose next cell to visit
        if (possibleDirections.Count > 0)
        {
            int chosenDirection = UnityEngine.Random.Range(0, possibleDirections.Count);
            MazeCell chosenCell = cells[possibleNextCells[chosenDirection]];

            //whichever direction we chose, knock down the wall on the way there
            switch (possibleDirections[chosenDirection])
            {
                //moving right
                case 1:
                    chosenCell.RemoveWall(1); //knock down left wall of chosen neighbor cell
                    currentPath[currentPath.Count - 1].RemoveWall(0); //knock down right wall of current cell
                    break;
                //moving left
                case 2:
                    chosenCell.RemoveWall(0); //knock down right wall of chosen neighbor cell
                    currentPath[currentPath.Count - 1].RemoveWall(1); //knock down left wall of current cell
                    break;
                //moving up
                case 3:
                    chosenCell.RemoveWall(3); //knock down bottom wall of chosen neighbor cell
                    currentPath[currentPath.Count - 1].RemoveWall(2); //knock down top wall of current cell
                    break;
                //moving down
                case 4:
                    chosenCell.RemoveWall(2); //knock down top wall of chosen neighbor cell
                    currentPath[currentPath.Count - 1].RemoveWall(3); //knock down bottom wall of current cell
                    break;
            }

            //add to our path
            currentPath.Add(chosenCell);
            //update cell's state
            chosenCell.SetState(MazeCellGenerationState.Current);
        }
        else //BACKTRACKING
        {
            //if no more cells available to visit, we backtrack to last place that did have unvisited neighbors
            completedCells.Add(currentPath[currentPath.Count - 1]);
            //as we backtrack, mark these cells as complete (also changes color)
            currentPath[currentPath.Count - 1].SetState(MazeCellGenerationState.Completed);
            //remove completed cells from our path
            currentPath.RemoveAt(currentPath.Count - 1);
        }
    }

    private void SetupStartAndEndOfMaze()
    {
        //mark start and end of maze (always start in lower left and end in upper right)

        //Maze start
        MazeCell startOfMaze = cells[0];
        //change color of starting square to make it obvious
        startOfMaze.SetState(MazeCellGenerationState.StartOfMaze);
        //knock out the left wall to create way into the maze
        //startOfMaze.RemoveWall(1);

        //Maze end
        MazeCell endOfMaze = cells[cells.Count - 1];
        //change color of starting square to make it obvious
        endOfMaze.SetState(MazeCellGenerationState.EndOfMaze);
        //knock out the right wall to create way out of the maze
        //endOfMaze.RemoveWall(0);
        Vector3 cheeseSpawnPos = endOfMaze.transform.position + new Vector3(0, 0.15f, 0);
        cheeseRef = Instantiate(prefabCheese, cheeseSpawnPos, Quaternion.identity, transform);

        //finished
        textGenNewMaze.SetActive(false);
    }

    public MazeCell GetStartOfMaze()
    {
        return cells[0];
    }

    public MazeCell GetEndOfMaze()
    {
        return cells[cells.Count - 1];
    }

    public MazeCell GetPlayerCurrentCell()
    {
        MazeCell playersCurrentCell = cells[0];

        foreach (MazeCell next in cells)
        {
            if (next.IsPlayerStandingHere())
            {
                playersCurrentCell = next;
                break;
            }
        }

        return playersCurrentCell;
    }

    private void SolveMaze_BFS(Vector2Int size, MazeCell start, MazeCell end)
    {
        //Find the solution using BFS algorithm (Queue with First-in-First-Out order)
        Queue<MazeCell> frontier = new Queue<MazeCell>();
        List<MazeCell> visited = new List<MazeCell>();
        List<MazeCell> solution = new List<MazeCell>();
        
        //works either way, but I start with end so path draws start-to-finish afterwards
        visited.Add(end);
        frontier.Enqueue(end);

        while (frontier.Count > 0)
        {
            MazeCell currentCell = frontier.Dequeue();

            if (currentCell == start)
            {
                //solved the maze, add this cell to our solution
                solution.Add(currentCell);

                //clear the Queue and breakout of loop
                frontier.Clear();
                break;
            }
            else
            {
                //foreach neighbor of curr:
                    //if neighbor is not already in visited && no wall btw cells
                        //frontier.Enqueue(neighbor);
                        //visited.Add(neighbor);
                        //neighbor.parentOfThisCell = curr;

                //add all unvisited neighbors of current cell to our Queue
                List<MazeCell> neighborCells = new List<MazeCell>();
                
                int currentCellIndex = cells.IndexOf(currentCell);
                int currentCellX = currentCellIndex / size.y;
                int currentCellY = currentCellIndex % size.y;

                //RIGHT
                if (currentCellX < size.x - 1)
                {
                    MazeCell right = cells[currentCellIndex + size.y];
                    if (IsThereAClearPathBetweenTheseTwoCells(size, currentCell, right)
                        && !visited.Contains(right))
                    {
                        visited.Add(right);
                        frontier.Enqueue(right);
                        neighborCells.Add(right);
                        right.SetParent(currentCell);
                    }
                }
                //LEFT
                if (currentCellX > 0)
                {
                    MazeCell left = cells[currentCellIndex - size.y];
                    if (IsThereAClearPathBetweenTheseTwoCells(size, currentCell, left)
                        && !visited.Contains(left))
                    {
                        visited.Add(left);
                        frontier.Enqueue(left);
                        neighborCells.Add(left);
                        left.SetParent(currentCell);
                    }
                }
                //UP
                if (currentCellY < size.y - 1)
                {
                    MazeCell up = cells[currentCellIndex + 1];
                    if (IsThereAClearPathBetweenTheseTwoCells(size, currentCell, up)
                        && !visited.Contains(up))
                    {
                        visited.Add(up);
                        frontier.Enqueue(up);
                        neighborCells.Add(up);
                        up.SetParent(currentCell);
                    }
                }
                //DOWN
                if (currentCellY > 0)
                {
                    MazeCell down = cells[currentCellIndex - 1];
                    if (IsThereAClearPathBetweenTheseTwoCells(size, currentCell, down)
                        && !visited.Contains(down))
                    {
                        visited.Add(down);
                        frontier.Enqueue(down);
                        neighborCells.Add(down);
                        down.SetParent(currentCell);
                    }
                }
            }
        }

        //Build out List<MazeCell> solution by following parent until no more parent
        MazeCell nextCellInSolution = solution[0];
        while (nextCellInSolution.GetParent() != null)
        {
            MazeCell newNext = nextCellInSolution.GetParent();
            solution.Add(newNext);
            nextCellInSolution = newNext;
        } 

        //Draw the maze solution so we can see it
        if (animateMazeGeneration)
        {
            StartCoroutine(DrawMazeSolution_Animated(solution));
        }
        else
        {
            DrawMazeSolution_Instant(solution);
        }
    }

    private bool IsThereAClearPathBetweenTheseTwoCells(Vector2Int size, MazeCell cellOne, MazeCell cellTwo)
    {
        //if the walls between these two cells are inactive, then true there is a clear path, otherwise false
        //cell.walls: 0 = right, 1 = left, 2 = top, 3 = bottom
        int cellOneIndex = cells.IndexOf(cellOne);
        int cellTwoIndex = cells.IndexOf(cellTwo);

        if (cellTwoIndex == cellOneIndex + size.y) //moving right?
        {
            if (!cells[cellOneIndex].walls[0].activeSelf && !cells[cellTwoIndex].walls[1].activeSelf)
            {
                return true;
            }
        }
        else if (cellTwoIndex == cellOneIndex - size.y) //moving left?
        {
            if (!cells[cellOneIndex].walls[1].activeSelf && !cells[cellTwoIndex].walls[0].activeSelf)
            {
                return true;
            }
        }
        else if (cellTwoIndex == cellOneIndex + 1) //moving up?
        {
            if (!cells[cellOneIndex].walls[2].activeSelf && !cells[cellTwoIndex].walls[3].activeSelf)
            {
                return true;
            }
        }
        else if (cellTwoIndex == cellOneIndex - 1) //moving down?
        {
            if (!cells[cellOneIndex].walls[3].activeSelf && !cells[cellTwoIndex].walls[2].activeSelf)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator DrawMazeSolution_Animated(List<MazeCell> soln)
    {
        foreach (MazeCell nextCell in soln)
        {
            if (nextCell.GetState() != MazeCellGenerationState.StartOfMaze
            && nextCell.GetState() != MazeCellGenerationState.EndOfMaze)
            {
                nextCell.SetState(MazeCellGenerationState.Solution);

                //brief delay in between operations so we can see the maze solution draw on cell at a time
                yield return new WaitForSeconds(delaySecondsBtwAnimatedMazeCellOperations);
            }
        }

        /*textSolveMaze.SetActive(false);
        buttonSpawnPlayerRat.SetActive(true);*/
    }

    private void DrawMazeSolution_Instant(List<MazeCell> mazeSolution)
    {
        foreach (MazeCell nextCell in mazeSolution)
        {
            if (nextCell.GetState() != MazeCellGenerationState.StartOfMaze
            && nextCell.GetState() != MazeCellGenerationState.EndOfMaze)
            {
                nextCell.SetState(MazeCellGenerationState.Solution);
            }
        }

        /*textSolveMaze.SetActive(false);
        buttonSpawnPlayerRat.SetActive(true);*/
    }

    public void SolveCurrentMaze()
    {
        SolveMaze_BFS(mazeSize, cells[0], cells[cells.Count - 1]);
    }

    public void SpawnPlayerRat()
    {
        //turn off camera that watches maze being built
        gameCamera.enabled = false;
        //game UI
        textGenNewMaze.SetActive(false);
        textPlayerControls.SetActive(true);

        Vector3 spawnPos = new Vector3(0f, 0.15f, 0f);
        playerRef = Instantiate(prefabPlayer, spawnPos, Quaternion.Euler(0, 45, 0));

        gameController.SetPlayerSolvingMaze(true);

        myCooldownForSniffing.gameObject.SetActive(true);
        myCooldownForSniffing.ResetAbility();
        myCooldownForPooping.gameObject.SetActive(true);
        myCooldownForPooping.ResetAbility();

        myCooldownForSniffing.ToggleBarVisibile(true);
        myCooldownForPooping.ToggleBarVisibile(true);
    }

    public void TempRevealMazeSolution(MazeCell start, MazeCell end)
    {
        //Find the solution using BFS algorithm (Queue with First-in-First-Out order)
        Queue<MazeCell> frontier = new Queue<MazeCell>();
        List<MazeCell> visited = new List<MazeCell>();
        List<MazeCell> solution = new List<MazeCell>();
        
        //works either way, but I start with end so path draws start-to-finish afterwards
        visited.Add(end);
        frontier.Enqueue(end);

        while (frontier.Count > 0)
        {
            MazeCell currentCell = frontier.Dequeue();

            if (currentCell == start)
            {
                //solved the maze, add this cell to our solution
                solution.Add(currentCell);

                //clear the Queue and breakout of loop
                frontier.Clear();
                break;
            }
            else
            {
                //foreach neighbor of curr:
                    //if neighbor is not already in visited && no wall btw cells
                        //frontier.Enqueue(neighbor);
                        //visited.Add(neighbor);
                        //neighbor.parentOfThisCell = curr;

                //add all unvisited neighbors of current cell to our Queue
                List<MazeCell> neighborCells = new List<MazeCell>();
                
                int currentCellIndex = cells.IndexOf(currentCell);
                int currentCellX = currentCellIndex / mazeSize.y;
                int currentCellY = currentCellIndex % mazeSize.y;

                //RIGHT
                if (currentCellX < mazeSize.x - 1)
                {
                    MazeCell right = cells[currentCellIndex + mazeSize.y];
                    if (IsThereAClearPathBetweenTheseTwoCells(mazeSize, currentCell, right)
                        && !visited.Contains(right))
                    {
                        visited.Add(right);
                        frontier.Enqueue(right);
                        neighborCells.Add(right);
                        right.SetParent(currentCell);
                    }
                }
                //LEFT
                if (currentCellX > 0)
                {
                    MazeCell left = cells[currentCellIndex - mazeSize.y];
                    if (IsThereAClearPathBetweenTheseTwoCells(mazeSize, currentCell, left)
                        && !visited.Contains(left))
                    {
                        visited.Add(left);
                        frontier.Enqueue(left);
                        neighborCells.Add(left);
                        left.SetParent(currentCell);
                    }
                }
                //UP
                if (currentCellY < mazeSize.y - 1)
                {
                    MazeCell up = cells[currentCellIndex + 1];
                    if (IsThereAClearPathBetweenTheseTwoCells(mazeSize, currentCell, up)
                        && !visited.Contains(up))
                    {
                        visited.Add(up);
                        frontier.Enqueue(up);
                        neighborCells.Add(up);
                        up.SetParent(currentCell);
                    }
                }
                //DOWN
                if (currentCellY > 0)
                {
                    MazeCell down = cells[currentCellIndex - 1];
                    if (IsThereAClearPathBetweenTheseTwoCells(mazeSize, currentCell, down)
                        && !visited.Contains(down))
                    {
                        visited.Add(down);
                        frontier.Enqueue(down);
                        neighborCells.Add(down);
                        down.SetParent(currentCell);
                    }
                }
            }
        }

        //Build out List<MazeCell> solution by following parent until no more parent
        MazeCell nextCellInSolution = solution[0];
        while (nextCellInSolution.GetParent() != null)
        {
            MazeCell newNext = nextCellInSolution.GetParent();
            solution.Add(newNext);
            nextCellInSolution = newNext;
        } 

        //Reveal scent markers temporarily
        StartCoroutine(TempShowMazeSolution(solution));
    }

    private IEnumerator TempShowMazeSolution(List<MazeCell> soln)
    {
        foreach (MazeCell nextCell in soln)
        {
            if (nextCell.GetState() != MazeCellGenerationState.StartOfMaze
            && nextCell.GetState() != MazeCellGenerationState.EndOfMaze)
            {
                nextCell.BrieflyRevealScentMarker();

                //brief delay in between operations so we can see the maze solution draw on cell at a time
                yield return new WaitForSeconds(delaySecondsBtwAnimatedMazeCellOperations);
            }
        }
    }

    public void TurnOnOverviewCamera()
    {
        gameCamera.enabled = true;
    }
}