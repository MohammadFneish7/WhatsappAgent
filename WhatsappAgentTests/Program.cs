// See https://aka.ms/new-console-template for more information
using WhatsappAgent;

// Start browser and login
Messegner Messegner = new Messegner(BrowserType.CHROME, login_timeout:100);
// Send image or video
Messegner.SendMedia(MediaType.IMAGE_OR_VIDEO, "70434962", "C:\\Users\\96170\\Desktop\\WhatsApp Image 2022-11-28 at 19.20.48.jpg", "this is an image with caption");
// Send attachment without caption
Messegner.SendMedia(MediaType.ATTACHMENT, "70434962", "C:\\Users\\96170\\Desktop\\WhatsApp Image 2022-11-28 at 19.20.48.jpg", "");
// Logout
Messegner.Logout();
