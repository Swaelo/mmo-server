// ================================================================================================================================
// File:        ObservationCamera.cs
// Description: Handles the controls and status of the servers observation camera
// Author:      Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Numerics;
using ContentRenderer;
using ServerUtilities;
using Server.Enums;
using Server.Logging;

namespace Server.Logic
{
    public class ObservationCamera
    {
        public Camera SceneCamera = null;
        public CameraMoveSpeedState CameraSpeedState = CameraMoveSpeedState.Regular;
        private bool CameraLock = false;

        public void SetCamera(GameLoop Loop)
        {
            SceneCamera = Loop.Camera;
        }

        public void PositionCamera(Vector3 Position, float Yaw, float Pitch)
        {
            SceneCamera.Position = Position;
            SceneCamera.Yaw = Yaw;
            SceneCamera.Pitch = Pitch;
        }

        public void MoveCamera(Controls UserControls, Input UserInput, float DeltaTime)
        {
            var CameraOffset = new Vector3();
            if (UserControls.MoveForward.IsDown(UserInput))
                CameraOffset += SceneCamera.Forward;
            if (UserControls.MoveBackward.IsDown(UserInput))
                CameraOffset += SceneCamera.Backward;
            if (UserControls.MoveLeft.IsDown(UserInput))
                CameraOffset += SceneCamera.Left;
            if (UserControls.MoveRight.IsDown(UserInput))
                CameraOffset += SceneCamera.Right;
            if (UserControls.MoveUp.IsDown(UserInput))
                CameraOffset += SceneCamera.Up;
            if (UserControls.MoveDown.IsDown(UserInput))
                CameraOffset += SceneCamera.Down;

            if (UserControls.LockMouse.WasTriggered(UserInput))
                CameraLock = !CameraLock;

            //Only update the camera position if its to be moved past a minimum distance
            float CameraMovement = CameraOffset.Length();
            if (CameraMovement > 1e-7f)
            {
                //Get the current camera movement speed
                float CameraMoveSpeed = 0;
                switch (CameraSpeedState)
                {
                    case CameraMoveSpeedState.Slow:
                        CameraMoveSpeed = UserControls.CameraSlowMoveSpeed;
                        break;
                    case CameraMoveSpeedState.Regular:
                        CameraMoveSpeed = UserControls.CameraMoveSpeed;
                        break;
                    case CameraMoveSpeedState.Fast:
                        CameraMoveSpeed = UserControls.CameraFastMoveSpeed;
                        break;
                }
                //Keep the movement speed framerate independant
                CameraOffset *= DeltaTime * CameraMoveSpeed / CameraMovement;
            }
            else
                CameraOffset = new Vector3();
            //Update the cameras position
            SceneCamera.Position += CameraOffset;

            //Use the mouse to turn the camera when the RMB is held down
            if (CameraLock)
            {
                var Delta = UserInput.MouseDelta;
                if (Delta.X != 0 || Delta.Y != 0)
                {
                    SceneCamera.Yaw += Delta.X * UserControls.MouseSensitivity;
                    SceneCamera.Pitch += Delta.Y * UserControls.MouseSensitivity;
                }
            }
        }

        public void UpdateCamera(Controls UserControls, Input UserInput, float DeltaTime)
        {
            //Adjust camera movement speed with the scrollwheel
            if (UserControls.MoveFaster.WasTriggered(UserInput))
            {
                switch (CameraSpeedState)
                {
                    case CameraMoveSpeedState.Slow:
                        CameraSpeedState = CameraMoveSpeedState.Regular;
                        break;
                    case CameraMoveSpeedState.Regular:
                        CameraSpeedState = CameraMoveSpeedState.Fast;
                        break;
                }
            }
            if (UserControls.MoveSlower.WasTriggered(UserInput))
            {
                switch (CameraSpeedState)
                {
                    case CameraMoveSpeedState.Regular:
                        CameraSpeedState = CameraMoveSpeedState.Slow;
                        break;
                    case CameraMoveSpeedState.Fast:
                        CameraSpeedState = CameraMoveSpeedState.Regular;
                        break;
                }
            }

            //Move camera around the world with WASD
            MoveCamera(UserControls, UserInput, DeltaTime);
        }
    }
}
