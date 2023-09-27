# WhatsappAgent
This is a Whatsapp message sending automation library using Selenium framework, based on the Whatsapp web app.
# Usage
```
using WhatsappAgent;

// Initialize driver and start browser (you can hide or show browser window)
Messegner Messegner = new Messegner(hideWindow: false);


void Messegner_OnQRReady(System.Drawing.Image qrbmp)
{
    // This is helpfull when 'hideWindow' option is set to true
    Console.WriteLine("Login QR code has been recieved as image, you can save it to disk or show it somewhere for the user so the user can scan it to continue login.");
}

void Messegner_OnDisposed()
{
    Console.WriteLine("Messenger has been disposed, you can't use it anymore.");
}

Messegner.OnDisposed += Messegner_OnDisposed;
Messegner.OnQRReady += Messegner_OnQRReady;

// Open web.whatsapp.com and try to login
Messegner.Login();

// Send text message
Messegner.SendMessage("70434962", "This is a text message");

// Send image or video
Messegner.SendMedia(MediaType.IMAGE_OR_VIDEO, "70434962", "C:\\Users\\96170\\Desktop\\WhatsApp Image 2022-11-28 at 19.20.48.jpg", "this is an image with caption");

// Send attachment without caption
Messegner.SendMedia(MediaType.ATTACHMENT, "70434962", "C:\\Users\\96170\\Desktop\\WhatsApp Image 2022-11-28 at 19.20.48.jpg", "");

// Logout from whatsapp and dispose the Messenger object
Messegner.Logout();

```

# Support
Please consider supporting my work [patreon](https://patreon.com/user?u=67136083&utm_medium=clipboard_copy&utm_source=copyLink&utm_campaign=creatorshare_creator&utm_content=join_link)
