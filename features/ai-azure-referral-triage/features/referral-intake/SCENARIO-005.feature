Feature: Reject Empty Referral Document

  Scenario: HTTP intake rejects document with zero bytes
    Given an HTTP POST endpoint at "/referrals/intake"
    And an empty file (0 bytes)
    When the POST request is sent with the empty file attached
    Then the HTTP response status is 400
    And the response body contains error message "Document is empty"
    And the response body contains "Minimum file size: 1 byte"
    And no ReferralId is assigned
    And the document is NOT stored in Blob Storage
    And no ReferralReceived domain event is emitted
