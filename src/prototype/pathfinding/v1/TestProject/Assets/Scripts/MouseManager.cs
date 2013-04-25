using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MouseButton
{
    Left = 0,
    Right,
    Middle
}

public enum ButtonState 
{
    Pressed,
    Released
}

public struct MouseEvent
{
    public MouseButton MouseButton;
    public Vector2 Location;
}

public interface IHandleMouseEvents
{
    void MouseClicked(MouseEvent e);
    void MousePressed(MouseEvent e);
    void MouseReleased(MouseEvent e);
}

public class MouseManager
{
	private Vector2 mousePosition; // this will always be in world coordinates
    private class MouseState
    {
        public ButtonState[] Buttons
        {
            get;
            private set;
        }

        public MouseState()
        {
            this.Buttons = new ButtonState[3];
            for(int ii = 0; ii < 3; ii++)
            {
                this.Buttons[ii] = Input.GetMouseButton(ii) ? ButtonState.Pressed : ButtonState.Released;
            }
        }
    }

    private MouseState previousMouseState = null;
    private IHandleMouseEvents mouseHandler;

    public MouseManager(IHandleMouseEvents handler)
    {
        this.mouseHandler = handler; 
		this.mousePosition = new Vector2(0.0f, 0.0f);
    }

    public void Update(float dt)
    {
        // Determine the current mouse state. 
        var currentMouseState = new MouseState();

        // Project the screen location of the mouse to world coordinates
        var scale = 1.0f / Futile.displayScale;
        var mouseX = Input.mousePosition.x;
        var mouseY = Input.mousePosition.y;
        var offsetX = -Futile.screen.originX * Futile.screen.pixelWidth;
        var offsetY = -Futile.screen.originY * Futile.screen.pixelHeight;
        //var worldX = (mouseX + offsetX) * scale;
        //var worldY = (mouseY + offsetY) * scale;
        var mouseEvent = new MouseEvent();
        //mouseEvent.Location = new Vector2(worldX, worldY);
		this.mousePosition.x = (mouseX + offsetX) * scale;
		this.mousePosition.y = (mouseY + offsetY) * scale;
		mouseEvent.Location = this.mousePosition;

        for(int ii = 0; ii < 3; ii++)
        {
            var curButtonState = currentMouseState.Buttons[ii];

            if (previousMouseState != null)
            {
                var prevButtonState = this.previousMouseState.Buttons[ii];
                mouseEvent.MouseButton = (MouseButton)ii;

                if (curButtonState == ButtonState.Pressed && prevButtonState == ButtonState.Released)
                {
                    this.mouseHandler.MousePressed(mouseEvent);
                }

                if (curButtonState == ButtonState.Released && prevButtonState == ButtonState.Pressed)
                {
                    this.mouseHandler.MouseReleased(mouseEvent);
                    this.mouseHandler.MouseClicked(mouseEvent);
                }
            }
        }

        previousMouseState = currentMouseState;
    }
}
