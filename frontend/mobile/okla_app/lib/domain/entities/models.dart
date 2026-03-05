/// Conversation entity for messaging
class Conversation {
  final String id;
  final String otherUserId;
  final String otherUserName;
  final String? otherUserAvatar;
  final String? vehicleId;
  final String? vehicleTitle;
  final String? vehicleImageUrl;
  final String? lastMessage;
  final DateTime? lastMessageAt;
  final int unreadCount;
  final bool isArchived;

  const Conversation({
    required this.id,
    required this.otherUserId,
    required this.otherUserName,
    this.otherUserAvatar,
    this.vehicleId,
    this.vehicleTitle,
    this.vehicleImageUrl,
    this.lastMessage,
    this.lastMessageAt,
    this.unreadCount = 0,
    this.isArchived = false,
  });
}

/// Chat message entity
class ChatMessage {
  final String id;
  final String conversationId;
  final String senderId;
  final String content;
  final DateTime sentAt;
  final bool isRead;
  final bool isFromMe;

  const ChatMessage({
    required this.id,
    required this.conversationId,
    required this.senderId,
    required this.content,
    required this.sentAt,
    this.isRead = false,
    this.isFromMe = false,
  });
}

/// Notification entity
class AppNotification {
  final String id;
  final String title;
  final String body;
  final String? type; // message, lead, price_alert, system, etc.
  final String? actionUrl;
  final String? imageUrl;
  final bool isRead;
  final DateTime createdAt;
  final Map<String, dynamic>? data;

  const AppNotification({
    required this.id,
    required this.title,
    required this.body,
    this.type,
    this.actionUrl,
    this.imageUrl,
    this.isRead = false,
    required this.createdAt,
    this.data,
  });
}

/// Review entity
class Review {
  final String id;
  final String reviewerId;
  final String reviewerName;
  final String? reviewerAvatar;
  final String targetId; // dealerId or sellerId
  final String targetType; // dealer, seller
  final int rating; // 1-5
  final String? comment;
  final String? response; // dealer/seller response
  final DateTime createdAt;
  final int helpfulCount;
  final bool isVerifiedPurchase;

  const Review({
    required this.id,
    required this.reviewerId,
    required this.reviewerName,
    this.reviewerAvatar,
    required this.targetId,
    required this.targetType,
    required this.rating,
    this.comment,
    this.response,
    required this.createdAt,
    this.helpfulCount = 0,
    this.isVerifiedPurchase = false,
  });
}

/// Dealer entity
class Dealer {
  final String id;
  final String name;
  final String? slug;
  final String? logoUrl;
  final String? bannerUrl;
  final String? description;
  final String? phone;
  final String? email;
  final String? website;
  final String? address;
  final String? province;
  final double? latitude;
  final double? longitude;
  final String? plan; // libre, visible, pro, elite
  final double rating;
  final int reviewCount;
  final int vehicleCount;
  final bool isVerified;
  final DateTime? createdAt;

  const Dealer({
    required this.id,
    required this.name,
    this.slug,
    this.logoUrl,
    this.bannerUrl,
    this.description,
    this.phone,
    this.email,
    this.website,
    this.address,
    this.province,
    this.latitude,
    this.longitude,
    this.plan,
    this.rating = 0,
    this.reviewCount = 0,
    this.vehicleCount = 0,
    this.isVerified = false,
    this.createdAt,
  });
}

/// Price alert entity
class PriceAlert {
  final String id;
  final String vehicleId;
  final String vehicleTitle;
  final String? vehicleImageUrl;
  final double targetPrice;
  final double currentPrice;
  final String currency;
  final bool isActive;
  final DateTime createdAt;

  const PriceAlert({
    required this.id,
    required this.vehicleId,
    required this.vehicleTitle,
    this.vehicleImageUrl,
    required this.targetPrice,
    required this.currentPrice,
    required this.currency,
    this.isActive = true,
    required this.createdAt,
  });

  bool get isTriggered => currentPrice <= targetPrice;
}

/// Appointment entity
class Appointment {
  final String id;
  final String vehicleId;
  final String vehicleTitle;
  final String dealerId;
  final String dealerName;
  final String? locationAddress;
  final DateTime scheduledAt;
  final String status; // pending, confirmed, cancelled, completed
  final String? notes;

  const Appointment({
    required this.id,
    required this.vehicleId,
    required this.vehicleTitle,
    required this.dealerId,
    required this.dealerName,
    this.locationAddress,
    required this.scheduledAt,
    this.status = 'pending',
    this.notes,
  });
}
