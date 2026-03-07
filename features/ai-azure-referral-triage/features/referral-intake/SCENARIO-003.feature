Feature: Reject Invalid File Type in Referral Intake

  Scenario: HTTP intake rejects unsupported file type (Word document)
    Given an HTTP POST endpoint at "/referrals/intake"
    And a Word document (.docx) containing referral information
    When the POST request is sent with the Word file attached
    Then the HTTP response status is 400
    And the response body contains error message "Unsupported file type"
    And the response body contains "Accepted types: PDF, plain text (.txt), scanned images (JPEG, PNG)"
    And no ReferralId is assigned
    And the document is NOT stored in Blob Storage
    And no ReferralReceived domain event is emitted
