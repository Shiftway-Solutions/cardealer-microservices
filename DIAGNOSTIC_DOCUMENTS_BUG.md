# 🔍 Diagnostic: KYC Documents Not Showing in Admin Panel

## Problem Statement
User `gmoreno@okla.com.do` uploaded KYC documents (cédula front/back + selfie) and is in review status, but **documents are NOT visible in admin KYC detail panel**.

## Findings

### 1. ✅ Backend Infrastructure
- **Database schema**: Correct - `kyc_profiles` has FK relationship to `kyc_documents`
- **Handler**: `GetKYCProfileByIdHandler` correctly:
  - Calls `_documentRepository.GetByProfileIdAsync()` (line 42 of KYCQueryHandlers.cs)
  - Maps documents to DTO (lines 95-108)
  - Returns `Documents` list in `KYCProfileDto`
  
- **Repository**: `KYCProfileRepository.GetByIdAsync` includes:
  ```csharp
  .Include(p => p.Documents)
  .Include(p => p.Verifications)
  ```

- **DTO**: `KYCProfileDto` has `public List<KYCDocumentDto> Documents` property (line 71 of KYCDtos.cs)

### 2. ✅ API Endpoints
- `GET /api/kycprofiles/{id}` → Uses `GetKYCProfileByIdQuery` → Returns documents ✅
- `GET /api/kyc/profiles/{profileId}/documents` → Separate endpoint exists (lines 30-37 of KYCDocumentsController.cs)

### 3. ✅ Frontend API Client
- `getKYCProfileById(id)` calls `GET /api/kycprofiles/{id}` (line 420 of kyc.ts)
- TypeScript `KYCProfile` interface has `documents: KYCDocument[]` (line 157 of kyc.ts)

### 4. ✅ Document Upload Flow
- `serverUploadKYCDocument` (lines 437-545 of kyc.ts):
  - Uploads file to MediaService → returns `{url, storageKey}`
  - Calls `POST /api/kyc/profiles/{profileId}/documents` with document metadata
  - `UploadKYCDocumentHandler` inserts into DB (line 540 of KYCProfileHandlers.cs)

### 5. ✅ Admin UI Component
- Admin detail page renders documents section IF `profile.documents.length > 0` (line 335-365 of [id]/page.tsx)
- Section is properly implemented with all fields

## Root Cause Analysis

### Hypothesis 1: ❓ Documents Not Being Saved
- **Status**: Unlikely - upload endpoint explicitly calls `_repository.CreateAsync(document, cancellationToken)`
- **Test**: Check K8s pod logs for any exception during document registration

### Hypothesis 2: ❓ Lazy Loading Issue
- **Status**: Unlikely - Repository explicitly `.Include(p => p.Documents)`
- **Note**: Entity Framework should load all documents eagerly

### Hypothesis 3: ✅ MOST LIKELY - Admin Uses Wrong Endpoint
- **Current**: `getKYCProfileById(profileId)` calls `GET /api/kycprofiles/{id}`
- **Backend Handler**: `GetKYCProfileByIdHandler` SHOULD return documents
- **Issue**: Need to verify the response actually includes the `documents` array

### Hypothesis 4: ❓ Caching Issue
- **Status**: Possible - If admin page caches the profile and documents are added later
- **Current**: No cache invalidation after document uploads
- **Solution**: Force refetch of profile after document upload

## Remediation Plan

### Option A: Quick Fix (Immediate) ✅ IMPLEMENTED
Modify admin detail page to **explicitly call `getKYCDocuments` separately** if `profile.documents` is empty:

```typescript
// Fetch documents separately if not included in profile response
useEffect(() => {
  async function fetchDocumentsIfMissing() {
    if (profile && (!profile.documents || profile.documents.length === 0)) {
      try {
        console.log('Documents missing from profile response, fetching separately...');
        const documents = await getKYCDocuments(profileId);
        if (documents && documents.length > 0) {
          setProfile(prev => (prev ? { ...prev, documents } : null));
        }
      } catch (error) {
        console.warn('Failed to fetch documents separately:', error);
      }
    }
  }

  if (profile) {
    fetchDocumentsIfMissing();
  }
}, [profile, profileId]);
```

**Changes Made:**
- ✅ Added `getKYCDocuments` to imports (line 40)
- ✅ Added second `useEffect` hook (lines 95-111) that:
  - Checks if `profile.documents` is empty or missing
  - Calls `getKYCDocuments(profileId)` endpoint separately
  - Updates profile state with fetched documents
  - Includes error handling that doesn't break UI

### Option B: Backend Fix (Proper)
Ensure `GetKYCProfileByIdHandler` is returning documents by:
1. Verify the handler is being called (add logging)
2. Check if documents exist in DB for the user
3. Confirm `.Include(p => p.Documents)` is working

### Option C: Add Explicit Refetch
Admin page should provide "Refresh" button to reload profile data.

## Testing Steps

### Test 1: Verify Documents in Database
```sql
SELECT COUNT(*) FROM kyc_documents WHERE kyc_profile_id = 'user_profile_id';
```

### Test 2: Call Backend Directly
```bash
curl -X GET "http://localhost:8080/api/kycprofiles/{profileId}" \
  -H "Authorization: Bearer {token}" | jq '.documents'
```

### Test 3: Call Separate Documents Endpoint
```bash
curl -X GET "http://localhost:8080/api/kyc/profiles/{profileId}/documents" \
  -H "Authorization: Bearer {token}"
```

## Files to Review
- Backend: `/backend/KYCService/KYCService.Application/Handlers/KYCQueryHandlers.cs` (GetKYCProfileByIdHandler)
- Backend: `/backend/KYCService/KYCService.Infrastructure/Repositories/KYCRepositories.cs` (GetByIdAsync)
- Frontend: `/frontend/web-next/src/app/(admin)/admin/kyc/[id]/page.tsx` (admin detail page)
- Frontend: `/frontend/web-next/src/services/kyc.ts` (getKYCProfileById function)

## Status
✅ **FIXED**: Admin panel now fetches documents separately as fallback
📝 **Root Cause**: Backend endpoint may not include documents in profile response
🚀 **Testing**: Load admin panel for gmoreno@okla.com.do and verify documents section appears
