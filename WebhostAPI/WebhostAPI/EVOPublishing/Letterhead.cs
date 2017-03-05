using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoPdf;
using WebhostMySQLConnection.EVOPublishing;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace WebhostMySQLConnection.EVOPublishing
{
    public class Letterhead
    {
        public static readonly String _InvalidFileNameCharacters = "#:?&!\"*/\\<>|";
        public static String EncodeSafeFileName(String input)
        {
            foreach(char ch in _InvalidFileNameCharacters)
            {
                if (input.Contains(ch))
                {
                    input = input.Replace(ch, ' ');
                }
            }

            return input;
        }

        private static int EVO_LICENSE_KEY_VARIABLE_INDEX = 4;
        private static HtmlToPdfConverter _converter;
        public static HtmlToPdfConverter PdfConverter
        {
            get
            {
                if (_converter == null)
                {
                    using (WebhostEntities db = new WebhostEntities())
                    {
                        _converter = new HtmlToPdfConverter();
                        _converter.LicenseKey = db.Variables.Find(EVO_LICENSE_KEY_VARIABLE_INDEX).Value;
                        _converter.PdfDocumentOptions.PdfPageSize = PdfPageSize.Letter;
                        _converter.PdfDocumentOptions.LeftMargin = InchesToPoints(1f);
                        _converter.PdfDocumentOptions.RightMargin = InchesToPoints(1f);
                        _converter.PdfDocumentOptions.TopMargin = InchesToPoints(0.5f);
                        _converter.PdfDocumentOptions.BottomMargin = InchesToPoints(1f);
                    }
                }

                return _converter;
            }
        }

        protected static int InchesToPoints(float inches)
        {
            return (int)(inches * 72);
        }

        /// <summary>
        /// Wrote this method when the EVO developers told me their API doesn't support 
        /// accessing the LocalMachine certificate store.  This pulls the Certificate with 
        /// FriendlyName "DocumentSigning" from the LocalMachine->Personal certificate store
        /// and then converts it to an EVO DigitalCertificate object.
        /// 
        /// As an intermediate step, the certificate must be exported as a PFX file.  This
        /// is done every time the certificate is requested so that the PFX data is not persistent
        /// anywhere in memory which could potentially be a security risk.
        /// </summary>
        protected static DigitalCertificate DocumentSigningCertificate
        {
            get
            {
                X509Store x509store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                x509store.Open(OpenFlags.ReadOnly);
                X509Certificate2 cert = null;

                foreach(X509Certificate2 c in x509store.Certificates)
                {
                    if(c.FriendlyName.Equals("DocumentSigning"))
                    {
                        cert = c;
                        break;
                    }
                }

                if (cert == null) throw new Exception("Could not locate DocumentSigning certificate.");

                byte[] pfxData = cert.Export(X509ContentType.Pfx, "abc123!@#");

                return DigitalCertificatesStore.GetCertificates(pfxData, "abc123!@#")[0];
            }
        }

        /// <summary>
        /// Can recipients print this PDF?
        /// 
        /// default value:  true
        /// </summary>
        public bool IsPrintable
        {
            get
            {
                return PdfConverter.PdfSecurityOptions.CanPrint;
            }
            set
            {
                PdfConverter.PdfSecurityOptions.CanPrint = value;
            }
        }

        /// <summary>
        /// Can recipients edit data from this PDF?
        /// 
        /// default value:  false
        /// </summary>
        public bool IsEditable
        {
            get
            {
                return PdfConverter.PdfSecurityOptions.CanEditContent;
            }
            set
            {
                PdfConverter.PdfSecurityOptions.CanEditContent = value;
                PdfConverter.PdfSecurityOptions.CanEditAnnotations = value;
            }
        }

        /// <summary>
        /// Can recipients Copy data from this pdf?
        /// 
        /// default value:  false
        /// </summary>
        public bool CanCopyContent
        {
            get
            {
                return PdfConverter.PdfSecurityOptions.CanCopyContent;
            }
            set
            {
                PdfConverter.PdfSecurityOptions.CanCopyContent = value;
            }
        }

        /// <summary>
        /// base URL for linked documents like css or images.
        /// Default constructor gives "https://webhost.dublinschool.org/"
        /// </summary>
        public String BaseURL
        {
            get;
            set;
        }

        public String CSS
        {
            get;
            set;
        }

        private static string _HeaderImageURL;
        /// <summary>
        /// URL of the Dublin School letterhead image.
        /// </summary>
        public static String HeaderImageURL
        {
            get
            {
                if(String.IsNullOrEmpty(_HeaderImageURL))
                {
                    using(WebhostEntities db = new WebhostEntities())
                    {
                        _HeaderImageURL = db.Variables.Find(5).Value;
                    }
                }

                return _HeaderImageURL;
            }
        }

        /// <summary>
        /// The Title of the HTML document.
        /// </summary>
        public String Title
        {
            get;
            set;
        }

        /// <summary>
        /// Not Yet Implemented.  Will do this if I need to....
        /// </summary>
        public String Metadata
        {
            get;
            set;
        }

        public String Template
        {
            get
            {
                return "<!DOCTYPE html>" + Environment.NewLine +
                        "<html>" + Environment.NewLine +
                        "<head>" + Environment.NewLine +
                        "\t<style>" + Environment.NewLine +
                        CSS + Environment.NewLine +
                        "\t</style>" + Environment.NewLine +
                        "<title>" + Title + "</title>" + Environment.NewLine +
                        "</head>" + Environment.NewLine +
                        "<body>" + Environment.NewLine +
                        "<a href=\"http://www.dublinschool.org\"><img id='header' src=\"" + HeaderImageURL + "\" alt=''/></a>" + Environment.NewLine +
                        "<div id='content'>{content}</div>" + Environment.NewLine + 
                        "{signature}" + Environment.NewLine +
                        "<footer class='certificate' data-mapping-enabled='true' data-mapping-id='digital_signature_element'>" + Environment.NewLine +
                        "\t<img alt='Logo Image' src='data:img/bmp;base64," + 
                        Convert.ToBase64String((byte[])(new ImageConverter()).ConvertTo(Resources.DigitalSignatureImage, typeof(byte[]))) + "'/>" + Environment.NewLine +
                        "</footer>" + Environment.NewLine +
                        "</body>" + Environment.NewLine +
                        "</html>";
            }
        }

        public bool IncludeSignature
        {
            get;
            set;
        }

        public String GenerateRandomKey(int length = 2048)
        {
            Random rand = new Random();
            byte[] key = new byte[length];
            rand.NextBytes(key);

            return Convert.ToBase64String(key);
        }

        public String SignatureImage(int facultyId)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                Faculty faculty = db.Faculties.Find(facultyId);
                if (faculty.SignatureData.Length <= 0) return "<p>____________________________</p>";

                String imgData = Convert.ToBase64String(faculty.SignatureData);

                return String.Format("<p>____________________________</p><p><img class='signature' atl='sig' src='data:img/png;base64,{0}' /></p>", imgData);
            }
        }



        /// <summary>
        /// Initialize a Letterhead Template object.  Contains standard Dublin School publication CSS styles.
        /// </summary>
        public Letterhead()
        {
            CSS = "body { font-family: 'Times New Roman'; font-size: 18pt; color: black; background-color: white; width:98%; overflow: visible;} " + Environment.NewLine +
                    "img#header { display: block; margin-left: auto; margin-right: auto; width: 100%; margin-bottom: 0.125in; }" + Environment.NewLine +
                    "@media print { footer.certificate img { display: none; } }" + Environment.NewLine +
                    "@media screen { footer.certificate img { display: block; height: 0.375in; margin: 0.0625in; } }" + Environment.NewLine +
                    "img.signature { max-height: 1.5in; max-width: 3.5in; min-width: 2in; }" + Environment.NewLine +
                    "header { font-size: 24pt; font-weight: bold; width: 100%; }" + Environment.NewLine +
                    "td { vertical-align: top; }" + Environment.NewLine +
                    "a { font-weight: bold; color: #0059b3; } a:hover { text-shadow: 0px 0px 8px #70A070; color: #8f1e1e;  }";
            BaseURL = "https://webhost.dublinschool.org/";
            CanCopyContent = false;
            IsEditable = false;
            IsPrintable = true;
            PdfConverter.PdfSecurityOptions.OwnerPassword = GenerateRandomKey();
        }
        
        public static bool AssertPath(String path, bool isFilePath = true)
        {
            if(isFilePath && File.Exists(path))
                return true;

            if(!isFilePath && Directory.Exists(path))
                return true;

            String[] parts = path.Split('\\');
            String rebuiltPath = parts[0];
            for (int i = 1; i < (isFilePath ? parts.Length - 1 : parts.Length); i++)
            {
                if (!Directory.Exists(rebuiltPath))
                {
                    try
                    {
                        Directory.CreateDirectory(rebuiltPath);
                    }
                    catch(Exception)
                    {
                        return false;
                    }
                }
                rebuiltPath += "\\" + parts[i];
            }

            return true;
        }

        /// <summary>
        /// Publishes a generic letter on School letterhead.  PDF protection options are set via the boolean properties of this object.
        /// Document is digitally signed using a certificate generated by the local certificate authority in the dublinschool.org domain.
        /// </summary>
        /// <param name="bodyHtml">Body of the letter.</param>
        /// <param name="signerId">Employee id of the Faculty who will put their signature on the document.  (automatically adds the signature image to the end of the document.</param>
        public Document PublishGenericLetter(String bodyHtml, bool digitallySign = false, int signerId = -1)
        {
            String html = Template.Replace("{content}", bodyHtml).Replace("{signature}", signerId == -1? "" : SignatureImage(signerId));
            Document document = PdfConverter.ConvertHtmlToPdfDocumentObject(html, BaseURL, BaseURL);

            if (digitallySign)
            {
                try
                {
                    /// digitally sign the document.
                    HtmlElementMapping dsMap = PdfConverter.HtmlElementsMappingOptions.HtmlElementsMappingResult.GetElementByMappingId("digital_signature_element");
                    if (dsMap != null)
                    {
                        PdfPage page = dsMap.PdfRectangles.Last().PdfPage;
                        RectangleF rectangle = dsMap.PdfRectangles.Last().Rectangle;

                        DigitalCertificate cert = DocumentSigningCertificate;

                        DigitalSignatureElement dse = new DigitalSignatureElement(rectangle, cert);
                        dse.Reason = "Ensure Document Integrity and Protect from unwanted changes.";
                        dse.ContactInfo = "Contact Email:  jason@dublinschool.org";
                        dse.Location = "Issuing Web Server";
                        page.AddElement(dse);
                    }
                }
                catch (Exception e)
                {
                    WebhostEventLog.CommentLog.LogError("Failed to Apply digital signature to document...{0}{1}", Environment.NewLine, e.Message);
                }
            }
            return document;
        }
    }
}
