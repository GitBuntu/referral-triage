Feature: Reject Oversized Referral Document

  Scenario: HTTP intake rejects document exceeding 5 MB size limit
    Given an HTTP POST endpoint at "/referrals/intake"
    And a PDF document with file size of 6 MB
    When the POST request is sent with the oversized PDF file attached
    Then the HTTP response status is 413
    And the response body contains error message "Request entity too large"
    And the response body contains "Maximum file size: 5 MB"
    And no ReferralId is assigned
    And the document is NOT stored in Blob Storage
    And no ReferralReceived domain event is emitted
