// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Timers;
using Toolbelt.Blazor.Gamepad;

namespace Microsoft.Xna.Framework.Input
{
    static partial class GamePad
    {
        private static GamepadList api;
        private static Timer timer = new Timer(1000 / 120);
        private static GamePadState current;
        private static bool hasStarted;
        private static bool startRequested;

        public static void Initialize(GamepadList blazorGamepad)
        {
            GamePad.api = blazorGamepad;
            Console.WriteLine("GamePad.Initialise");
        }

        private static async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var gamepads = await api.GetGamepadsAsync();

                if (gamepads.Count > 0)
                {
                    current = Convert(gamepads[0]);
                }

                if (!hasStarted)
                {
                    hasStarted = true;
                    Console.WriteLine("GamePad Started.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                timer.Stop();
            }

            static GamePadState Convert(Toolbelt.Blazor.Gamepad.Gamepad state)
            {
                Buttons buttons = 0;
                for (int i = 0; i < state.Buttons.Count; i++)
                {
                    if (state.Buttons[i].Pressed)
                    {
                        //Console.WriteLine(i + " pressed");
                        buttons |= i switch
                        {
                            //17 => touchpad
                            16 => Buttons.BigButton,
                            15 => Buttons.DPadRight,
                            14 => Buttons.DPadLeft,
                            13 => Buttons.DPadDown,
                            12 => Buttons.DPadUp,
                            11 => Buttons.RightStick,
                            10 => Buttons.LeftStick,
                            9 => Buttons.Start,
                            8 => Buttons.Back,
                            7 => Buttons.RightTrigger,
                            6 => Buttons.LeftTrigger,
                            5 => Buttons.RightShoulder,
                            4 => Buttons.LeftShoulder,
                            3 => Buttons.Y,
                            2 => Buttons.X,
                            1 => Buttons.B,
                            0 => Buttons.A,
                            _ => 0
                        };
                    }
                }

                var deadZone = 0.05;

                return new GamePadState
                {
                    Buttons = new GamePadButtons(buttons),
                    DPad = new GamePadDPad(buttons),
                    ThumbSticks =
                        state.Axes.Count == 4
                            ? new GamePadThumbSticks(
                                new Vector2(DeadZone(state.Axes[0]), -DeadZone(state.Axes[1])),
                                new Vector2(DeadZone(state.Axes[2]), -DeadZone(state.Axes[3])))
                            : new GamePadThumbSticks(),
                };

                float DeadZone(double value) =>
                    Math.Abs(value) <= deadZone
                        ? 0
                        : (float)(value * (1 + deadZone)) - (float)(Math.Sign(value) * deadZone);
            }
        }

        static GamePad()
        {
        }

        private static int PlatformGetMaxNumberOfGamePads()
        {
            if (startRequested == false)
            {
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                startRequested = true;
                Console.WriteLine("GamePad Start Requested...");
            }

            return current != default ? 1 : 0;
        }

        private static GamePadCapabilities PlatformGetCapabilities(int index)
        {
            throw new NotImplementedException();
        }

        private static GamePadState PlatformGetState(int index, GamePadDeadZone leftDeadZoneMode, GamePadDeadZone rightDeadZoneMode)
        {
            return current;
        }

        private static bool PlatformSetVibration(int index, float leftMotor, float rightMotor, float leftTrigger, float rightTrigger)
        {
            throw new NotImplementedException();
        }
    }
}
