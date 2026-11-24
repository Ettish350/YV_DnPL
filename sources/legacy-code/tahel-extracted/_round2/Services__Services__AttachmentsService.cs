using Data.Modals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class AttachmentsService : IAttachmentsService
    {
        private readonly IAttachmentRepository attachmentRepository;
        private readonly IQueryHelperRepository queryHelperRepository;
        public AttachmentsService(IAttachmentRepository attachmentRepository,
            IQueryHelperRepository queryHelperRepository)
        {
            this.attachmentRepository = attachmentRepository;
            this.queryHelperRepository = queryHelperRepository;
        }
        public List<Attachment> SaveAttachments(string relatedItemId, List<Attachment> attachmentList, string userId)
        {
            foreach (var attachment in attachmentList)
            {
                attachment.RelatedEntityName = queryHelperRepository.FirstCharToUpper(attachment.RelatedEntityName);
                if (attachment.RelatedEntityName == new Objects().Institution)
                {
                    attachment.Institution = new IdValuePair() { Id = relatedItemId };
                }
                else if (attachment.RelatedEntityName == new Objects().Reservation)
                {
                    attachment.Reservation = new IdValuePair() { Id = relatedItemId };
                }
                else if (attachment.RelatedEntityName == new Objects().EventReservation)
                {
                    attachment.EventReservation = new IdValuePair() { Id = relatedItemId };
                }
                else if (attachment.RelatedEntityName == new Objects().EventRegistration)
                {
                    attachment.EventRegistration = new IdValuePair() { Id = relatedItemId };
                }
                attachmentRepository.CreateOrUpdateEntity(attachment, null, userId);
            }
            return attachmentList;
        }
    }
}
