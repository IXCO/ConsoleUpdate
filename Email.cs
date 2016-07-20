using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limilabs.Mail;
using Limilabs.Client.POP3;
using Limilabs.Mail.MIME;
using System.Net.Mail;
using RestSharp;
using RestSharp.Authenticators;
namespace ConsoleUpdate
{
    class Email
    {

        public string date;
        public string from;
        public string subject;
        
        public Email(String senderEmail,String addedDate)
        {
            from = senderEmail;
            date = addedDate;
           
        }
        public Email(){
        }
        public IRestResponse sendComposeMail(String content, String idInvoice, String society)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");

            client.Authenticator =
                new HttpBasicAuthenticator("api", key);
            RestRequest request = new RestRequest();

            request.AddParameter("domain",
                "mg.ixco.com.mx", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            //Gets class information
            request.AddParameter("from", "Facturación Electrónica <" + sendAccount + ">");
            request.AddParameter("to", from);
            request.AddParameter("subject", subject);
            //Checks if the message is an authorization or an error
            String contentHtml;
            if (subject.Contains("Error"))
            {
                contentHtml = "<head><style> .cuerpo{color:#848484;  font-size:14px;  height: 200px;  width: 400px;" +
            "padding:15px;  line-height:20px; margin-left:15px;}" +
            ".pie{  font-size:11px;  padding:15px;margin-left:45px;}" +
            ".footer{  border-top:1px solid;  border-color:#FFBF00;  height: 100px;  width: 400px; margin-left:5px; }" +
            ".encabezado{  border-bottom:10px solid;  border-color:#FFBF00;  height: 100px;  width: 400px;  color:#A4A4A4; " +
            "font-family:Verdana, Geneva, sans-serif;  padding:10px; margin-left:5px;}" +
            "img{margin-left:115px;} a{ text-decoration: none; color:#EEBD2B;} </style></head>" +
            "<div class='encabezado'><img width='115' height='85' alt='logo' src='http://192.168.20.66/facturacion/protexa.png'> </div>" +
            "<div class='cuerpo'>El sistema de recepción de factura electrónica detecto un error al procesar el correo del día " + date +
            ", por lo que no se puede proceder con el trámite correspondiente.<br> " +
            "<b>Error detectado</b> : '" + content + "'<br> " +
            "Favor de revisar y reenviar el correo a la dirección <a href='mailto:" + account + "'>" + account + "</a> .<br>" +
            "Cualquier duda favor de comunicarse con nosotros:<br>" +
            "<a>Tel.</a> (51) (81) 87 48 17 00 <div class='footer'>" +
            "<p class='pie'> Este es un aviso automático. Favor de no responder</p> </div></div>";
            }
            else
            {
                contentHtml = "<head><style>" +
            ".cuerpo{color:#848484;  font-size:14px;  height: 230px;  width: 400px;" +
            "padding:15px;  line-height:20px; margin-left:15px;}" +
            ".pie{  font-size:11px;  padding:15px;margin-left:45px;}" +
            ".footer{  border-top:1px solid;  border-color:#FFBF00;  height: 100px;  width: 400px; margin-left:5px; }" +
            ".encabezado{  border-bottom:10px solid;  border-color:#FFBF00;  height: 100px;  width: 400px;  color:#A4A4A4; " +
            "font-family:Verdana, Geneva, sans-serif;  padding:10px; margin-left:5px;}" +
            "img{margin-left:115px;} a{ text-decoration: none; color:#EEBD2B;} </style></head>" +
            "<div class='encabezado'><img width='115' height='85' alt='logo' src='http://192.168.20.66/facturacion/protexa.png'> </div>" +
            "<div><p class='cuerpo'>Buen día, <br> " +
            content + "Dicha solicitud se encuentra a la espera de su autorización.<br>Usted desea:<br><br>" +
            "<a href='http://192.168.20.66/facturacion/autorizacion/autorizacion.php?fact=" + idInvoice + "&sol=" + society + "'> Aceptar" +
            "<img width='30' height= '30' src='http://192.168.20.66/facturacion/checkmark.png'></a>  <br><br>" +
            "<a href='http://192.168.20.66/facturacion/sinaprobacion/sinaprobacion.php?fact=" + idInvoice + "&sol=" + society + "'> Negar" +
            "<img width='30' height= '30' src='http://192.168.20.66/facturacion/X_mark.gif'></a>" +
            "<br>Anexo a este correo puede encontrar la representación impresa de la factura, de lo contrario el proveedor no la proporciono." +
             "<br>Cualquier duda o falla con el correo favor de <a href='mailto:"+adminAccount+"'>contactarnos</a></p><div class='footer'>" +
            "<p class='pie'> Este es un aviso automático. Favor de no responder</p> </div></div>";
                Archivo file = new Archivo();
                //Checks if invoice has a PDF file already downloaded
                String nameOFFile = file.hasPDFForAttachment(idInvoice);
                if (nameOFFile != null)
                {
                    //If it has file then it is attached to petition
                    request.AddFile("attachment", nameOFFile);
                }
            }
            request.AddParameter("html", contentHtml);

            request.Method = Method.POST;
            return client.Execute(request);
        }
    }
}
