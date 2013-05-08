﻿using System;
using System.Collections.Generic;
using UnityEngine;

#region Event Args

public class TouchEventArgs : EventArgs 
{
    public FTouch Touch
    {
        get;
        set;
    }
}

#endregion

/// <summary>
/// This class implements the "Mission" scene, where the turned based strategy stuff happens.
/// Still under HEAVY development.
/// </summary>
public class MissionScene : GameScene, FSingleTouchableInterface
{
    #region Events
    
    public event EventHandler<TouchEventArgs> TouchEnded;

    #endregion

    #region Private Fields

    // Some book-keeping lists/sets
    private HashSet<Vector2i> localAttackablePoints;
    private HashSet<Vector2i> highlightedIndicies;

    private FSprite crosshair;
    private FSprite[,] overlay;
    private ButtonStrip buttonStrip;
    private PhaserShotAnimation phaserShot;
   
    #endregion

    #region Public Properties

    public PathfindResult Pathfinding
    {
        get;
        private set;
    }

    public Actor SelectedActor
    {
        get;
        private set;
    }

    public Map Map
    {
        get;
        private set;
    }
    
    #endregion

    #region Start

    /// <summary>
    /// Initialize resources.  Right now, for testing purposes, a simple random map is 
    /// generated here with a few actors in it.
    /// </summary>
    public override void Start()
    {
        var width = 50;
        var height = 35;
        var tiles = new TileProperties[width, height];
        this.localAttackablePoints = new HashSet<Vector2i>();

        // Generate a very simple random map with two different kinds of tiles
        var rand = new System.Random();
        for (int ii = 0; ii < width; ii++)
        {
            for (int jj = 0; jj < height; jj++)
            {
                tiles[ii, jj] = (rand.NextDouble() < 0.7) ? StaticTiles.GrassTile : StaticTiles.ForestTiles;
            }
        }
        this.Map = new Map(tiles);

        // Add a few actors to the map
        var actor1 = new Actor("goodsoldier");
        actor1.MovementPoints = 6;
        actor1.Name = "Hunkenheim";
        actor1.IsComputer = false;
        actor1.TurnState = ActorState.TurnStart;
        actor1.Abilities.Add(new Move());
        actor1.Abilities.Add(new Wait());
        this.Map.AddActor(actor1, new Vector2i(5, 5));

        var actor2 = new Actor("goodsoldier");
        actor2.MovementPoints = 6;
        actor2.Name = "Hunkenheim";
        actor2.IsComputer = false;
        actor2.TurnState = ActorState.TurnStart;
        actor2.Abilities.Add(new Move());
        actor2.Abilities.Add(new Wait());
        this.Map.AddActor(actor2, new Vector2i(5, 6));

        var actor3 = new Actor("goodsoldier");
        actor3.MovementPoints = 6;
        actor3.Name = "Hunkenheim";
        actor3.IsComputer = false;
        actor3.TurnState = ActorState.TurnStart;
        actor3.Abilities.Add(new Move());
        actor3.Abilities.Add(new Wait());
        this.Map.AddActor(actor3, new Vector2i(5, 7));
        
        this.Map.Start();
        this.AddChild(this.Map);

        this.crosshair = new FSprite("crosshair");
        this.crosshair.width = this.crosshair.height = GameConstants.TileSize;

        // Initialize highlights
        this.highlightedIndicies = new HashSet<Vector2i>();
        this.overlay = new FSprite[Map.Columns, Map.Rows];
        for (int ii = 0; ii < Map.Columns; ii++)
        {
            for (int jj = 0; jj < Map.Rows; jj++)
            {
                var overlaySprite = new FSprite("bluehighlight");
                overlaySprite.x = ii * GameConstants.TileSize + GameConstants.TileSize / 2;
                overlaySprite.y = jj * GameConstants.TileSize + GameConstants.TileSize / 2;
                overlaySprite.isVisible = false;
                overlaySprite.width = overlaySprite.height = GameConstants.TileSize;
                this.overlay[ii, jj] = overlaySprite;
                this.AddChild(overlaySprite);
            }
        }
    }

    #endregion

    #region Update

    /// <summary>
    /// Update does a few things at the moment:
    /// - Handles mouse-over events (look at the position of the mouse and figure out if 
    ///   we have to change the display in some way).
    /// - Handles key presses
    /// </summary>
    public override void  Update()
    {
        var mousePosition2d = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        var globalCoords = MouseManager.ScreenToGlobal(mousePosition2d);
        var gridVector = this.Map.GlobalToGrid(globalCoords);
        this.scrollMap(mousePosition2d);

        if (this.SelectedActor != null)
        {
            var actorState = this.SelectedActor.TurnState;

            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (actorState == ActorState.AwaitingCommand)
                {
                }
            }
        }

        base.Update();
    }

    #endregion 

    #region Overrides

    override public void HandleAddedToStage()
    {
        base.HandleAddedToStage();
        Futile.touchManager.AddSingleTouchTarget(this);
    }

    override public void HandleRemovedFromStage()
    {
        base.HandleRemovedFromStage();
        Futile.touchManager.RemoveSingleTouchTarget(this);
    }

    #endregion 

    #region Public Methods

    public void SetOverlay(int x, int y, string sprite)
    {
        this.overlay[x, y].SetElementByName(sprite);
    }

    public void SetOverlayVisible(int x, int y, bool visible)
    {
        this.overlay[x, y].isVisible = visible;

        var vector = new Vector2i(x,y);
        if (visible)
        {
            this.highlightedIndicies.Add(vector);
        }
        else
        {
            if (this.highlightedIndicies.Contains(vector))
            {
                this.highlightedIndicies.Remove(vector);
            }
        }
    }

    public void ClearOverlay()
    {
        foreach (var index in this.highlightedIndicies)
        {
            this.overlay[index.X, index.Y].isVisible = false;
        }
        this.highlightedIndicies.Clear();
    }

    #endregion

    #region Touch Handlers

    public bool HandleSingleTouchBegan(FTouch touch)
    {
        if (this.SelectedActor != null && this.SelectedActor.TurnState == ActorState.AwaitingCommand)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void HandleSingleTouchMoved(FTouch touch)
    {
    }

    public void HandleSingleTouchEnded(FTouch touch)
    {
        var handler = this.TouchEnded;
        if (handler != null)
        {
            handler(this, new TouchEventArgs() { Touch = touch });
        }

        var gridVector = this.Map.GlobalToGrid(touch.position);
        Actor tempActor;
        Debug.Log("Touch at: " + touch.position + ", map grid: " + gridVector);
          
        if (this.SelectedActor != null)
        {
            var actorState = this.SelectedActor.TurnState;
            //if(actorState == ActorState.SelectingEnemy)
            //{
            //    if (this.localAttackablePoints.Contains(gridVector))
            //    {
            //        Debug.Log("Attacking point: " + gridVector);
            //        Vector2 source = new Vector2(this.SelectedActor.x, this.SelectedActor.y);
            //        Vector2 dest = this.Map.GridToGlobal(gridVector);
            //        this.shootLaser(source, dest);
            //    }
            //}
        }
        else if(this.Map.TryGetActor(gridVector, out tempActor) && !tempActor.IsComputer)
        {
            this.SelectedActor = tempActor;
            var actorState = this.SelectedActor.TurnState;
            if (actorState == ActorState.TurnStart)
            {
                this.SelectedActor.TurnState = ActorState.AwaitingCommand;
                this.Pathfinding = MoveAndAttackHelper.Pathfind(this.Map, this.SelectedActor);
                this.showButtonStrip(this.SelectedActor);
            }
        }
    }

    public void HandleSingleTouchCanceled(FTouch touch)
    {
    }

    #endregion 

    #region Button Presses

    private void abilityButtonPressed(FButton b)
    {
        var ability = b.data as Ability;
        ability.AbilityComplete += new EventHandler<AbilityCompleteEventArgs>(abilityComplete);
        this.RemoveChild(this.buttonStrip);
        this.buttonStrip = null;
        ability.Activate(this);
    }

    private void abilityComplete(object sender, AbilityCompleteEventArgs args)
    {
        if (args.TurnOver)
        {
            this.SelectedActor.TurnState = ActorState.TurnOver;
            this.SelectedActor = null;
            this.ClearOverlay();
        }
        else
        {
            this.SelectedActor.TurnState = ActorState.AwaitingCommand;
            this.showButtonStrip(this.SelectedActor);
        }
    }

    private void attackButton_SignalRelease(FButton b)
    {
        Debug.Log("Attack button clicked.");
        if (this.SelectedActor != null)
        {
            this.highlightLocalAttackable();
            this.SelectedActor.TurnState = ActorState.SelectingEnemy;
            this.RemoveChild(this.buttonStrip);
        }
    }

    #endregion

    #region Private Methods

    private void shootLaser(Vector2 start, Vector2 end)
    {
        var phaserShot = new PhaserShotAnimation(start, end);
        phaserShot.Start();
        phaserShot.Delay = 10;
        phaserShot.AnimationStopped += new EventHandler<AnimationEventArgs>(this.phaserShot_AnimationStopped);
        this.AddChild(phaserShot);
        phaserShot.Play();
    }

    private void phaserShot_AnimationStopped(object sender, AnimationEventArgs args)
    {
        if (args.StoppedReason == StoppedReason.Complete)
        {
            Debug.Log("Removing phaser " + sender);
            this.RemoveChild(sender as FNode);
        }
    }

    private void clearCrossHair()
    {
        if (this._childNodes.Contains(this.crosshair))
        {
            this.RemoveChild(this.crosshair);
        }
    }

    private void clearSelection()
    {
        this.ClearOverlay();
        this.SelectedActor = null;
    }

    private void showButtonStrip(Actor actor)
    {
        this.buttonStrip = new ButtonStrip();
        this.buttonStrip.Orientation = ButtonStripOrientation.Horizontal;
        this.buttonStrip.Spacing = 3.0f;

        if (actor != null && actor.TurnState == ActorState.AwaitingCommand)
        {
            foreach (var ability in actor.AvailableAbilities)
            {
                var button = new FButton(ability.IconName);
                button.sprite.width = button.sprite.height = 32;
                button.data = ability;
                button.SignalRelease += new Action<FButton>(abilityButtonPressed);
                this.buttonStrip.AddButton(button);
            }
        }

        this.buttonStrip.x = actor.x + actor.width;
        this.buttonStrip.y = actor.y - actor.height;
        this.AddChild(this.buttonStrip);
    }

    /// <summary>
    /// Draw a crosshair at the given position in the grid.
    /// </summary>
    /// <param name="dest"></param>
    private void drawCrossHair(Vector2i dest)
    {
        this.crosshair.x = dest.X * GameConstants.TileSize + GameConstants.TileSize / 2;
        this.crosshair.y = dest.Y * GameConstants.TileSize + GameConstants.TileSize / 2;

        if (!this._childNodes.Contains(this.crosshair))
        {
            this.AddChild(this.crosshair);
        }
    }

    private void highlightLocalAttackable()
    {
        /*
        this.clearHighlights();
        foreach (var item in this.localAttackablePoints)
        {
            this.overlay[item.X, item.Y].SetElementByName("redhighlight");
            this.overlay[item.X, item.Y].isVisible = true;
            this.highlightedIndicies.Add(item);
        }
         * */
    }

    private void scrollMap(Vector2 mousePosition)
    {
        var mX = mousePosition.x;
        var mY = mousePosition.y;
        var margin = GameConstants.MapScrollMargin;

        if ((mX >= 0 && mX < margin) || Input.GetKey(KeyCode.A))
        {
            if (this.x < 0)
            {
                this.x += GameConstants.MapScrollSpeed;
            }
        }

        if ((mX > (Futile.screen.width - margin) && mX <= Futile.screen.width) || Input.GetKey(KeyCode.D))
        {
            if (Math.Abs(this.x) + Futile.screen.width < this.Map.Width)
            {
                this.x -= GameConstants.MapScrollSpeed;
            }
        }

        if ((mY > 0 && mY < margin) || Input.GetKey(KeyCode.S))
        {
            if (this.y < 0)
            {
                this.y += GameConstants.MapScrollSpeed;
            }
        }

        if ((mY > (Futile.screen.height - margin) && mY < Futile.screen.height) || Input.GetKey(KeyCode.W))
        {
            if (Math.Abs(this.y) + Futile.screen.height < this.Map.Height)
            {
                this.y -= GameConstants.MapScrollSpeed;
            }
        }
    }

    #endregion
}