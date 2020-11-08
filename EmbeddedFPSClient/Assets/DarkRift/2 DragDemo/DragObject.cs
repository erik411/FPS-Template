﻿using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    /// <summary>
    ///     The tag indicating a movement of the object.
    /// </summary>
    const ushort MOVE_TAG = 0;

    [SerializeField]
    [Tooltip("The Client to communicate with the server via.")]
    UnityClient client;

    [SerializeField]
    [Tooltip("The ID to identify this object across the network using")]
    byte dragID;

    [SerializeField]
    [Tooltip("The speed at which the object will drag at.")]
    float speed = 15;
    
    /// <summary>
    ///     This will be an object used to smoothly move between positions.
    /// </summary>
    Vector3 targetPosition;

    void Awake ()
    {
        //Check we have a Client to send/receive from
        if (client == null)
        {
            Debug.LogError("No Client assigned to DragObject!");
            return;
        }

        //Subscribe to the event for when we receive messages
        client.MessageReceived += Client_MessageReceived;

        //Set our default target position to our current position
        targetPosition = transform.position;
	}

    void Update()
    {
        //Lerp between positions to create a smoother transition
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed);
    }

    void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            //Check the message has a zero tag
            if (message.Tag == MOVE_TAG)
            {
                //Get the reader from the message so we can read the data
                using (DarkRiftReader reader = message.GetReader())
                {
                    //If it's for us...
                    if (reader.ReadByte() == dragID)
                    {
                        //... then update our position!
                        targetPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), 0);
                    }
                }
            }
        }
    }
	
    //Called when the object is dragged by the mouse
	void OnMouseDrag ()
    {
        //Check we have a Client to send from
        if (client == null)
        {
            Debug.LogError("No Client assigned to DragObject!");
            return;
        }

        //Firstly we need to work out where the object should be, we can ignore the z-coord returned
        Vector3 newPos = Camera.main.ScreenPointToRay(Input.mousePosition).GetPoint(10);

        //We want to send the new position of the object to the other clients so we write our ID and 
        //our position (as x, y and z components) into a DarkRiftWriter
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(dragID);
            writer.Write(newPos.x);
            writer.Write(newPos.y);

            //Then we'll create a new message and put the DarkRiftWriter into it.
            //The tag indicates what the message is about so we'll put a tag of '0' to indicate a
            //movement.
            using (Message message = Message.Create(MOVE_TAG, writer))
            {
                //We can then send the message
                client.SendMessage(message, SendMode.Unreliable);
            }
        }

        //Last but not least we'll actually move the object on our screen so set the target position to 
        //the new position
        targetPosition = new Vector3(newPos.x, newPos.y, 0);
	}
}
