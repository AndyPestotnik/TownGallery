using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.Events;

public class SavePhoto : MonoBehaviour
{
    public UnityEvent Function_onPicked_Return; // Visual Scripting trigger [On Unity Event]
    public UnityEvent Function_onSaved_Return; // Visual Scripting trigger [On Unity Event]

    public NativeFilePicker.Permission permission; // Permission to access Camera Roll

    public void SavePhotoToCameraRoll(Texture2D MyTexture,string AlbumName, string filename)
    {
        NativeGallery.SaveImageToGallery(MyTexture, AlbumName, filename , (callback, path) => 
        {
            if (callback == false)
            {
                Debug.Log("Failed to save !");
            }
            else
            {
                Debug.Log("Photo is saved to Camera Roll on phone device.");

                Function_onSaved_Return.Invoke(); // Triggered [On Unity Event] in Visual Scripting
            }
        
        });

    }

    public void PickPhotoCameraRoll()
    {
        
        if ( permission == NativeFilePicker.Permission.Granted)
        {
            Debug.Log("Permission Granted");
            NativeFilePicker.PickFile((path) =>
            {
                if (path == null)
                {
                    Debug.Log("Pick Photo : Canceled");
                }
                else
                {
                    Debug.Log("Pick Photo : Success");
                    Variables.ActiveScene.Set("Picked File", path);

                    Function_onPicked_Return.Invoke(); // Triggered [On Unity Event] in Visual Scripting
                }
            }, "image/*");
        }
        else
        {
            Debug.Log("Permission Not Granted");
            AskPermission();
            
        }

    }

    public async void AskPermission()
    {
        NativeFilePicker.Permission permissionResult = await NativeFilePicker.RequestPermissionAsync(false);
        permission = permissionResult;

        if (permission == NativeFilePicker.Permission.Granted)
        {
            PickPhotoCameraRoll();
        }
    }
}
