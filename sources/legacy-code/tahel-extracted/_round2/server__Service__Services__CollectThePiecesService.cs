using Data.Models;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CollectThePiecesService : ICollectThePiecesService
    {
        private readonly ICRMConnection _crmService;
        private readonly ICollectThePiecesRepository _collectThePiecesRepository;
        private readonly IHelperService _helperService;


        public CollectThePiecesService(ICRMConnection crmConnection, ICollectThePiecesRepository collectThePiecesRepository, IHelperService helperService, IHttpClientFactory factory)
        {
            _crmService = crmConnection;
            _collectThePiecesRepository = collectThePiecesRepository;
            _helperService = helperService;
        }

        public async Task<bool> CreateCollectThePiecesInSapirAsync(string collectThePiecesGuid)
        {
            try
            {
                Log.Information($"CreateCollectThePiecesInSapir start collectThePiecesGuid: {collectThePiecesGuid}");

                string itemIdInSapir = "";
                string donorsNames = "";
                List<string> donorsEmails = new List<string>();
                string errorUpdatingSapirAfterCreating = "";
                var fetchXml = _collectThePiecesRepository.StartFetchXml("new_collect_the_pieces");
             
                fetchXml +=
                $@"<attribute name='new_restrictions_on_use'/> <attribute name='new_language_for_email'/> <attribute name='new_provenance'/> <attribute name='new_family_biography'/> <attribute name='new_barcode'/> <attribute name='new_collection_type'/> <attribute name='new_collection_date'/> <attribute name='new_name_of_owner_pices'/> <attribute name='new_name_of_owner_pices_en'/> <attribute name='new_place_of_owner_pices'/> <attribute name='new_place_of_owner_pices_en'/> <attribute name='new_birth_date_of_owner_pices'/> <filter type='and'> <condition attribute='new_collect_the_piecesid' operator='eq' uitype='new_collect_the_pieces' value='" + collectThePiecesGuid + @"'/> </filter> <link-entity name='new_absorbing_material' from='new_collect_the_pieces_id' to='new_collect_the_piecesid' alias='AbsorbingMaterial'> <attribute name='new_special_requests'/> <attribute name='new_restrictions_on_use'/> <attribute name='new_memory_amount_copy'/> <attribute name='new_memory_amount'/> <attribute name='new_object_amount'/> <attribute name='new_diary_amount_copy'/> <attribute name='new_diary_amount'/> <attribute name='new_works_of_art_amount'/> <attribute name='new_other_amount'/> <attribute name='new_other_amount_copy'/> <attribute name='new_letter_amount_copy'/> <attribute name='new_letter_amount'/> <attribute name='new_document_amount_copy'/> <attribute name='new_document_amount'/> <attribute name='new_book_amount'/> <attribute name='new_film_amount_copy'/> <attribute name='new_orign_film_amount'/> <attribute name='new_testimony_amount_copy'/> <attribute name='new_testimony_amount'/> <attribute name='new_survivor_form_amount_copy'/> <attribute name='new_survivor_form_amount'/> <attribute name='new_photo_amount_copy'/> <attribute name='new_photo_amount'/> <attribute name='new_testimony_amount'/> <attribute name='new_albums_amount'/> <attribute name='new_testimony_pages_amount'/> <attribute name='new_absorbing_materialid' /> <attribute name='new_incoming_transferred' /> <attribute name='createdon' /> <attribute name='new_is_not_for_sending_an_email' /> <attribute name='new_hall_of_names_photo_amount' /> <attribute name='new_hall_of_names_photo_amount_copy' /> <attribute name='new_music_recording_amount' /> <attribute name='new_music_recording_amount_copy' /> <attribute name='new_production_film_amount' /> <attribute name='new_production_film_amount_copy' /> <filter type='and'><condition attribute='statecode' operator='eq' value='0' /></filter></link-entity>";
                fetchXml += _collectThePiecesRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var collectThePiecesResult = _crmService.RetrieveMultiple(query);
                if (collectThePiecesResult.Entities.Count() > 0)
                {
                    if ((collectThePiecesResult.Entities[0].GetAttributeValue<AliasedValue>("AbsorbingMaterial.new_incoming_transferred")?.Value.ToString() ?? "False") == "False")
                        throw new Exception("AbsorbingMaterial.new_incoming_transferred is False");

                    fetchXml = _collectThePiecesRepository.StartFetchXml("new_collect_the_pieces_donor");
                    fetchXml +=
                    $@"<attribute name='new_collect_the_pieces_donorid'/> <attribute name='new_contact_relationship_optionset'/> <attribute name='new_donor_first_name_en'/> <attribute name='new_donor_last_name_en'/> <attribute name='new_copyright_holder'/> <attribute name='new_on_behalf_of_copyright_holder'/> <attribute name='new_contact_for_more_information'/> <filter type='and'> <condition attribute='new_collect_the_pieces' operator='eq' value='" + collectThePiecesGuid + @"'/> <condition attribute='statecode' operator='eq' value='0' /> </filter> <link-entity name='contact' from='contactid' to='new_contact' alias='Contact'> <attribute name='firstname'/> <attribute name='lastname'/> <attribute name='address1_line1'/> <attribute name='address1_city'/> <attribute name='emailaddress1'/> <attribute name='mobilephone'/> <attribute name='telephone1'/> </link-entity>";
                    fetchXml += _collectThePiecesRepository.EndFetchXml();
                    query = new FetchExpression(fetchXml);
                    var donorsResult = _crmService.RetrieveMultiple(query);
                    if (donorsResult.Entities.Count() > 0)
                    {
                        var result = await _collectThePiecesRepository.CreateCollectThePiecesInSapirAsync(collectThePiecesResult.Entities[0], donorsResult.Entities);
                        if (result.Item1.Contains(";"))
                        {
                            itemIdInSapir = result.Item1.Split(';')[0];
                            errorUpdatingSapirAfterCreating = result.Item1.Split(';')[1];
                            if (errorUpdatingSapirAfterCreating == "")
                                errorUpdatingSapirAfterCreating = "Unknown error";
                        }
                        else
                        {
                            itemIdInSapir = result.Item1;
                        }
                        donorsNames = result.Item2;
                        donorsEmails = result.Item3;
                        if (donorsEmails.Count < 1)
                            donorsEmails.Add("anna.israel@yadvashem.org.il");

                    }
                    else
                    {
                        throw new Exception("There is no collectThePieces donors");
                    }
                }
                else
                {
                    throw new Exception("There is no collectThePieces or absorbingMaterial with specific Guid");
                }
                if (itemIdInSapir != "" && itemIdInSapir.All(Char.IsDigit))
                {
                    try
                    {
                        Entity entity = new Entity("new_collect_the_pieces");
                        entity.Id = Guid.Parse(collectThePiecesGuid);
                        entity["new_is_exists_in_sapir"] = true;
                        _crmService.Update(entity);

                        entity = new Entity("new_absorbing_material");
                        entity.Id = Guid.Parse(collectThePiecesResult.Entities[0].GetAttributeValue<AliasedValue>("AbsorbingMaterial.new_absorbing_materialid").Value.ToString());
                        entity["new_incoming_transferred_number"] = itemIdInSapir;
                        entity["new_incoming_transferred_date"] = DateTime.Now;
                        _crmService.Update(entity);
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"Error in updating fields in CRM after creating an item in Sapir: {ex}");
                        throw new Exception("Error in updating fields in CRM after creating an item in Sapir");
                    }
                    if (errorUpdatingSapirAfterCreating != "")
                    {
                        Log.Information($"Error in updating MoreInfo after creating an item in Sapir: {errorUpdatingSapirAfterCreating}");
                        throw new Exception("Error in updating MoreInfo after creating an item in Sapir " + errorUpdatingSapirAfterCreating);
                    }

                    if ((collectThePiecesResult.Entities[0].GetAttributeValue<AliasedValue>("AbsorbingMaterial.new_is_not_for_sending_an_email")?.Value.ToString() ?? "False") == "True")
                    {
                        Log.Information($"dont send email!!!!");
                        //אם מסומן לא לשלוח אימייל לתורמים, אז לשלוח אימייל רק לאנה
                        //send email only to anna
                        donorsEmails.Clear();
                        Log.Information($"after remove all donors emails"+ donorsEmails.Count);
                        donorsEmails.Add("anna.israel@yadvashem.org.il");
                        Log.Information($"after add anna's email" + donorsEmails.Count);


                    }
                        try
                        {
                            using (StreamReader r = new StreamReader("C:/crmconnection/JsonFiels/EmailText.json"))
                            {
                                string language = "he";
                                if ((collectThePiecesResult.Entities[0].GetAttributeValue<OptionSetValue>("new_language_for_email")?.Value.ToString() ?? "1") == "2")
                                    language = "en";
                                if ((collectThePiecesResult.Entities[0].GetAttributeValue<OptionSetValue>("new_language_for_email")?.Value.ToString() ?? "1") == "3")
                                    language = "ru";
                                DateTime today = DateTime.Now;
                                var ci = CultureInfo.CreateSpecificCulture("he-IL");
                                ci.DateTimeFormat.Calendar = new HebrewCalendar();
                                string hebrewDate = today.ToString("dd", ci) + " " + today.ToString("y", ci);

                                var json = JObject.Parse(r.ReadToEnd());
                                string description = json[language]["description"].ToString().Replace("dateToRepalce", today.ToString("dd/MM/yyyy") + " - " + hebrewDate).Replace("donorsNamesToReplace", donorsNames).Replace("wonerNameToReplace", (collectThePiecesResult.Entities[0].GetAttributeValue<string>("new_name_of_owner_pices") ?? string.Empty) + " (" + (collectThePiecesResult.Entities[0].GetAttributeValue<string>("new_name_of_owner_pices_en") ?? string.Empty) + ")").Replace("numberInSapirToReplace", itemIdInSapir).Replace("barcodeToReplace", collectThePiecesResult.Entities[0].GetAttributeValue<string>("new_barcode"));
                                MailInfo mailInfo = new MailInfo();
                                string subject = json[language]["subject"].ToString().Replace("numberInSapirToReplace", itemIdInSapir);
                                mailInfo.Message = description;
                                mailInfo.Subject = subject;
                              //  mailInfo.To = new List<string>() { "orit.noiman@yadvashem.org.il" };
                                mailInfo.To = donorsEmails;
                                mailInfo.Cc = new List<string>();
                                mailInfo.Bcc = new List<string>() { "yv.dev.logs@yadvashem.org.il", "incoming.gratitude@yadvashem.org.il" };
                                mailInfo.From = "collect@yadvashem.org.il";
                                mailInfo.Files = new List<string>();
                                mailInfo.Direction = Direction.RTL;

                                _helperService.CreateRequest("http://reservations.yadvashem/sendmail/mail", "POST", JsonConvert.SerializeObject(mailInfo));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"Error sending email: {ex.Message}");
                            throw new Exception("Error sending email " + ex.Message);
                        }
                        try
                        {
                            Entity entity = new Entity("new_absorbing_material");
                            entity.Id = Guid.Parse(collectThePiecesResult.Entities[0].GetAttributeValue<AliasedValue>("AbsorbingMaterial.new_absorbing_materialid").Value.ToString());
                            entity["new_is_send_letter"] = true;
                            entity["new_send_letter_date"] = DateTime.Now;
                            _crmService.Update(entity);
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"Error in updating fields in CRM after sending email: {ex}");
                            throw new Exception("Error in updating fields in CRM after sending email");
                        }
                    
                   
                }
                else
                {
                    throw new Exception("Error creating an item in Sapir "+itemIdInSapir);
                }
                Log.Information($"CreateCollectThePiecesInSapir end");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"CreateCollectThePiecesInSapir error, {ex.Message}.");
                throw new Exception(ex.Message);
            }
        }

    }
}
