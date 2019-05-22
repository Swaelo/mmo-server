// ================================================================================================================================
// File:        SceneSwapper.cs
// Description: 
// ================================================================================================================================

using ContentRenderer.UI;
using ServerUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Server.Scenes
{
    struct SceneSwapper
    {
        public int TargetSceneIndex;
        bool TrackingInput;

        public void CheckForSceneSwap(SceneHarness harness)
        {
            if (harness.input.WasPushed(harness.controls.ChangeScene.Key))
            {
                TrackingInput = !TrackingInput;
                TargetSceneIndex = -1;
            }

            if (TrackingInput)
            {
                for (int i = 0; i < harness.input.TypedCharacters.Count; ++i)
                {
                    var character = harness.input.TypedCharacters[i];
                    if (character == '\b')
                    {
                        //Backspace!
                        if (TargetSceneIndex >= 10)
                            TargetSceneIndex /= 10;
                        else
                            TargetSceneIndex = -1;
                    }
                    else
                    {
                        if (TargetSceneIndex < harness.sceneSet.Count)
                        {
                            var digit = character - '0';
                            if (digit >= 0 && digit <= 9)
                            {
                                TargetSceneIndex = Math.Max(0, TargetSceneIndex) * 10 + digit;
                            }
                        }
                    }
                }

                if (harness.input.WasPushed(OpenTK.Input.Key.Enter))
                {
                    //Done entering the index. Swap the demo if needed.
                    TrackingInput = false;
                    harness.TryChangeToScene(TargetSceneIndex);
                }
            }
        }

        public void Draw(TextBuilder text, TextBatcher textBatcher, SceneSet sceneSet, Vector2 position, float textHeight, Vector3 textColor, Font font)
        {
            if (TrackingInput)
            {
                text.Clear().Append("Swap demo to: ");
                if (TargetSceneIndex >= 0)
                    text.Append(TargetSceneIndex);
                else
                    text.Append("_");
                textBatcher.Write(text, position, textHeight, textColor, font);

                var lineSpacing = textHeight * 1.1f;
                position.Y += textHeight * 0.5f;
                textHeight *= 0.8f;
                for (int i = 0; i < sceneSet.Count; ++i)
                {
                    position.Y += lineSpacing;
                    text.Clear().Append(sceneSet.GetName(i));
                    textBatcher.Write(text.Clear().Append(i).Append(": ").Append(sceneSet.GetName(i)), position, textHeight, textColor, font);
                }
            }

        }
    }
}
