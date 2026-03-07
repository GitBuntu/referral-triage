Feature: Accept Valid PDF Referral Document

  Scenario: HTTP intake accepts valid PDF document and returns ReferralId
    Given an HTTP POST endpoint at "/referrals/intake"
    And a valid PDF referral document (2 MB) with existing binary content
    When the POST request is sent with the PDF file attached
    Then the HTTP response status is 201
    And the response body contains a valid UUID in the "referralId" field
    And the response body contains a valid RFC3339 timestamp in the "timestamp" field
    And the raw PDF document is stored in Blob Storage at path "/referrals/incoming/{referralId}"
    And a ReferralReceived domain event is emitted with the ReferralId in the event payload
