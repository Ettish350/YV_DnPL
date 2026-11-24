using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Services.Services
{
    public class ContactService : BasicService<Contact, Contact, ContactCRM>, IContactService

    {

        private readonly IContactRepository contactRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IQueryHelperRepository queryHelperRepository;

        public ContactService(IHelperService helperService,
            IContactRepository contactRepository,
            IMapFunctionRepository mapFunctionRepository,
            IQueryHelperRepository queryHelperRepository)
             : base(helperService, contactRepository, mapFunctionRepository)
        {
            this.contactRepository = contactRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.queryHelperRepository = queryHelperRepository;
        }


        public List<Contact> GetAll(string userId, List<Filter> filterList)
        {            
            try
            {
                var sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<Contact, ContactCRM>("FirstName"),
                    Direction = "asc"
                };

                return contactRepository.GetFetchXml(userId, false, null, new List<Sort> { sort }, filterList).Item2;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public void FixAllContactDeskEntities(string currentDesk, string newDesk, string owner, List<string> countries)
        {
            string userID = "BCF7A729-155F-E611-80CE-005056B13C97";//crm service user

            string countriesXml = "";
            foreach (var country in countries)
            {
                countriesXml += $@"<condition attribute='new_name' operator='eq' value='{country}'/>";
            }
            string contactDeskXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='contact'>
    <attribute name='fullname' />
    <attribute name='address1_city' />
    <attribute name='new_deskid' />
    <attribute name='new_country' />
    <attribute name='address1_line3' />
    <attribute name='address1_line2' />
    <attribute name='address1_line1' />
    <attribute name='telephone1' />
    <attribute name='telephone2' />
    <attribute name='contactid' />
    <attribute name='emailaddress1' />
    <order attribute='fullname' descending='false' />
    <order attribute='new_country' descending='false' />
    <filter type='and'>
        <condition attribute='statecode' operator='eq' value='0'/>
    </filter>
    <link-entity name='new_country' from='new_countryid' to='new_country' alias='country'>
        <filter type='or'>
         {countriesXml}
        </filter>
    </link-entity>
<link-entity name='new_country1' from='new_country1id' to='new_country' alias='country1'>
        <filter type='or'>
         {countriesXml}
        </filter>
    </link-entity>
<link-entity name='new_country2' from='new_country2id' to='new_country' alias='country2'>
        <filter type='or'>
         {countriesXml}
        </filter>
    </link-entity>
    <link-entity name='new_desk' from='new_deskid' to='new_deskid' alias='ak'>
        <attribute name='new_deskid'/>
        <filter type='and'>
            <condition attribute='new_name' operator='eq' value='{currentDesk}'/>
        </filter>
    </link-entity>
  </entity>
</fetch>";

            var list = basicRepository.GetAllListEntity("", contactDeskXML);

            string newDeskXML = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                    <entity name='new_desk'>
                                            <attribute name='new_deskid'/>
                                            <filter type='and'>
                                                <condition attribute='new_name' operator='eq' value='{newDesk}'/>
                                            </filter>
                                    </entity>
                                  </fetch>";

            var newDeskItem = basicRepository.GetAllListEntity("",newDeskXML).First();
            string newDeskId = newDeskItem.Id.ToString();

            string ownerXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                    <entity name='team'>                                   
                                        <attribute name='teamid'/>
                                        <filter type='and'>
                                            <condition attribute='teamtype' operator='eq' value='0'/>
                                            <condition attribute='name' operator='eq' value='{owner}'/>
                                        </filter>
                                    </entity>
                                 </fetch>";

            var ownerItem = basicRepository.GetAllListEntity("",ownerXml).First();
            string ownerId = ownerItem.Id.ToString();

            int errorCount = 0;
            int i = 0;
            foreach (var item in list)
            {
                i++;
                try
                {
                    var details = queryHelperRepository.GetAllValuesOfEntity(item);
                    string itemId = details["contactid"];
                    string currentDeskId = details["ak.new_deskid"];

                    UpdateCrmItem updateCrmItemPreviousDesk = new UpdateCrmItem
                    {
                        Field = "new_contact_old_desk",
                        Value = currentDeskId,
                        Type = "lookup",
                        EntityReferenceType = "new_desk"
                    };
                    basicRepository.UpdateField(itemId, updateCrmItemPreviousDesk, userID);

                    UpdateCrmItem updateCrmItemNewDesk = new UpdateCrmItem
                    {
                        Field = "new_deskid",
                        Value = newDeskId,
                        Type = "lookup",
                        EntityReferenceType = "new_desk"
                    };
                    basicRepository.UpdateField(itemId, updateCrmItemNewDesk, userID);

                    basicRepository.UpdateAssignRequestToEntity(itemId, ownerId, userID, true);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Trace.WriteLine($"{item["fullname"]} {item["emailaddress1"]} contact error");
                }
            }
        }




        private List<Contact> InternalServerError(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
