using System;
using Android.Content;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Platform;
using Android.App;


namespace Microsoft.Xna.Framework
{
    internal class ScreenReceiver : BroadcastReceiver
    {	
        public static bool ScreenLocked;
        
        public override void OnReceive(Context context, Intent intent)
        {
            Android.Util.Log.Info("Kni", intent.Action.ToString());
            if(intent.Action == Intent.ActionScreenOff)
            {
                OnLocked();
            }
            else if(intent.Action == Intent.ActionScreenOn)
            {
                // If the user turns the screen on just after it has automatically turned off, 
                // the keyguard will not have had time to activate and the ActionUserPreset intent
                // will not be broadcast. We need to check if the lock is currently active
                // and if not re-enable the game related functions.
                // http://stackoverflow.com/questions/4260794/how-to-tell-if-device-is-sleeping
                KeyguardManager keyguard = (KeyguardManager)context.GetSystemService(Context.KeyguardService);
                if (!keyguard.InKeyguardRestrictedInputMode())
                    OnUnlocked();
            }
            else if(intent.Action == Intent.ActionUserPresent)
            {
                // This intent is broadcast when the user unlocks the phone
                OnUnlocked();
            }

            if (intent.Action == Android.Telephony.TelephonyManager.ActionPhoneStateChanged)
            {
                if (intent.Extras != null)
                {
                    string state = intent.GetStringExtra(Android.Telephony.TelephonyManager.ExtraState);
                    if (state == Android.Telephony.TelephonyManager.ExtraStateRinging)
                    {
                        // TODO: Find a way to set Game.IsActive = false during a call.
                        // View.ClearFocus() doesn't have any affect. 
                        // The best we can do currently is to sent the game to foreground.
                        AndroidGameWindow.Activity.MoveTaskToBack(true);
                    }
                }
            }
        }

        private void OnLocked()
        {
            ScreenReceiver.ScreenLocked = true;
            MediaPlayer.IsMuted = true;
        }

        private void OnUnlocked()
        {
            ScreenReceiver.ScreenLocked = false;
            MediaPlayer.IsMuted = false;
            ((AndroidGameWindow)ConcreteGame.GameConcreteInstance.Window).GameView.Resume();
        }
    }
}

