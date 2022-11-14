using Godot;
using System;
using System.Collections.Generic;

public class GhostScript : CharacterScript
{
    private Movement moveScr = new Movement();
    private Vector2[] nodeList;
    private Vector2[] adjList;
    private int mazeOrigin;
    private List<Vector2> paths;
    private KinematicBody2D pacman;
    private TileMap nodeTilemap;
    private int mazeheight;
    private int Gspeed = 100;
    private int pathCounter = 0;
    private bool recalculate = false;
    Vector2 movementV;
    protected override void MoveAnimManager(Vector2 masVector)
    {
        AnimatedSprite ghostEyes = GetNode<AnimatedSprite>("GhostEyes"); //not sure whether to put it in here for readabillity or in each ready so theres less calls
        GD.Print(masVector);
        masVector = masVector.Normalized();
        GD.Print(masVector);
        masVector = masVector.Round();

        GD.Print(masVector);
        if (masVector == Vector2.Up)
        {
            ghostEyes.Play("up");
        }
        else if (masVector == Vector2.Down)
        {
            ghostEyes.Play("down");
        }
        else if (masVector == Vector2.Right)
        {
            ghostEyes.Play("right");
        }
        else if (masVector == Vector2.Left)
        {
            ghostEyes.Play("left");
        }
    }
    //As GhostScript is a base class, it will not be in the scene tree so ready and process are not needed
    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
    {
        GD.Print("ghostscript ready");

        mazeTm = GetParent().GetNode<TileMap>("MazeTilemap");
        nodeTilemap = GetParent().GetNode<TileMap>("NodeTilemap");
        pacman = GetNode<KinematicBody2D>("/root/Game/Pacman");

        nodeList = (Vector2[])mazeTm.Get("nodeList");
        adjList = (Vector2[])mazeTm.Get("adjList");
        mazeOrigin = (int)mazeTm.Get("mazeOriginY");
        mazeheight = (int)mazeTm.Get("height");




        Position = new Vector2(1, mazeOrigin + 1) * 32 + new Vector2(16, 16); //spawn ghost on top left of current maze


        paths = moveScr.Dijkstras(mazeTm.WorldToMap(Position), mazeTm.WorldToMap(pacman.Position), nodeList, adjList);
        GD.Print("ready paths count", paths.Count);

        GD.Print("world to map pos ", mazeTm.WorldToMap(Position));
        GD.Print("map to world wtm pos ", mazeTm.MapToWorld(mazeTm.WorldToMap(Position)));
        GD.Print("curr pos ", Position);
        GD.Print("ready pacman pos", pacman.Position);

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    private void OnResetChasePathTimeout()
    {
        recalculate = true; //every x seconds, set recalculate to true
    }

    private bool IsOnNode(Vector2 pos)
    {

        if (nodeTilemap.GetCellv(mazeTm.WorldToMap(pos)) == Globals.NODE)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private Vector2 FindClosestNodeTo(Vector2 nodeVector)
    {
        //the node must have the same x or y as targetPos
        int shortestInt = Globals.INFINITY;
        Vector2 shortestNode = Vector2.Inf;

        foreach (Vector2 node in nodeList)
        {
            if ((node.y == nodeVector.y || node.x == nodeVector.x) && (node != nodeVector))
            {
                int currShortestInt = Math.Abs(moveScr.ConvertVecToInt(nodeVector - node));
                if (currShortestInt < shortestInt)
                {
                    shortestInt = currShortestInt;
                    shortestNode = node;
                }

            }
        }

        return shortestNode;
    }
    private void FindNewPath(Vector2 sourcePos, Vector2 targetPos)
    {
        pathCounter = 0;

        //if targetpos is not in nodeList && is within bound of curr maze (essentially, if pacman is between nodes:)
        //dont do this garbage, use the coordinates method instead
        if ((!Array.Exists(nodeList, element => element == targetPos)) && (targetPos.y < (mazeOrigin + mazeheight - 3)))
        {
            GD.Print("THIS IS GETTING CALLED!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            GD.Print("targetpos ", targetPos);
            GD.Print("mazeorigin+height-3 ", mazeOrigin + mazeheight - 3);
            //search for the closest vector in nodelist to targetPos that is not targetpos, make that = targetPos.
            targetPos = FindClosestNodeTo(targetPos);

        }

        paths = moveScr.Dijkstras(sourcePos, targetPos, nodeList, adjList);
    }

    private void GhostChase(float delta)
    {
        if (IsOnNode(Position) && recalculate) //every x seconds, if pacman and ghost is on a node, it recalulates shortest path.
        {
            recalculate = false;
            FindNewPath(mazeTm.WorldToMap(Position), mazeTm.WorldToMap(pacman.Position));
        }

        if (pathCounter < paths.Count)
        {
            if (Position.IsEqualApprox(mazeTm.MapToWorld(paths[pathCounter]) + new Vector2(16, 16))) //must use IsEqualApprox with vectors due to floating point precision errors instead of ==
            {
                pathCounter++; //if ghost position == node position then increment
            }
            else
            {
                movementV = Position.MoveToward(mazeTm.MapToWorld(paths[pathCounter]) + new Vector2(16, 16), delta * Gspeed); //if not, move toward node position
                Position = movementV;
                MoveAnimManager(paths[pathCounter] - mazeTm.WorldToMap(Position));

                // GD.Print("Position ", Position);
            }

            //GD.Print(pathCounter);

        }
        else if (pathCounter >= paths.Count) //if its reached the end of its path, calculate new path
        {
            FindNewPath(mazeTm.WorldToMap(Position), mazeTm.WorldToMap(pacman.Position));
        }
    }
    public override void _Process(float delta)
    {
        PlayAndPauseAnim(movementV);
        GhostChase(delta);
    }
}

