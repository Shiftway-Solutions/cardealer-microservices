---
title: "73 - Common Static Pages"
priority: P0
estimated_time: ""
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 📄 73 - Common Static Pages

**Objetivo:** Páginas estáticas comunes: FAQ, Cómo Funciona, Escribir Reseña.

**Prioridad:** P2 (Media)  
**Complejidad:** 🟢 Baja  
**Dependencias:** ReviewService (para WriteReview)

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [FAQPage](#-faqpage)
3. [HowItWorksPage](#-howitworkspage)
4. [WriteReviewPage](#-writereviewpage)

---

## 🏗️ ARQUITECTURA

```
pages/
├── FAQPage.tsx                   # Preguntas frecuentes (293 líneas)
├── HowItWorksPage.tsx            # Cómo funciona (299 líneas)
└── WriteReviewPage.tsx           # Escribir reseña (254 líneas)

services/
└── reviewService.ts              # API de reseñas
```

---

## ❓ FAQPAGE

**Ruta:** `/faq` o `/help/faq`

```typescript
// src/pages/FAQPage.tsx
import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import MainLayout from '@/layouts/MainLayout';
import { FiSearch, FiChevronDown, FiChevronUp, FiHelpCircle } from 'react-icons/fi';

// FAQ Categories
type FAQCategory = 'buying' | 'selling' | 'account' | 'payments' | 'general';

interface FAQItem {
  id: string;
  question: string;
  answer: string;
  category: FAQCategory;
}

// Static FAQ Data
const FAQ_ITEMS: FAQItem[] = [
  // Buying
  {
    id: 'buy-1',
    question: '¿Cómo puedo buscar un vehículo?',
    answer: 'Puedes usar nuestra barra de búsqueda avanzada con filtros por marca, modelo, año, precio, kilometraje y más. También puedes navegar por categorías o usar las secciones destacadas en la página principal.',
    category: 'buying',
  },
  {
    id: 'buy-2',
    question: '¿Cómo contacto a un vendedor?',
    answer: 'En la página del vehículo encontrarás botones para llamar, enviar WhatsApp o mensaje interno. También puedes usar el formulario de contacto. Necesitas crear una cuenta para enviar mensajes.',
    category: 'buying',
  },
  {
    id: 'buy-3',
    question: '¿OKLA verifica los vehículos?',
    answer: 'Los dealers verificados pasan por un proceso de verificación KYC. Sin embargo, recomendamos siempre inspeccionar el vehículo personalmente antes de comprar y solicitar un historial vehicular.',
    category: 'buying',
  },
  {
    id: 'buy-4',
    question: '¿Puedo guardar vehículos favoritos?',
    answer: 'Sí, al crear una cuenta puedes guardar vehículos en favoritos y recibir alertas cuando cambien de precio o cuando haya vehículos similares disponibles.',
    category: 'buying',
  },
  // Selling
  {
    id: 'sell-1',
    question: '¿Cómo publico mi vehículo?',
    answer: 'Crea una cuenta, ve a "Vender" y completa el formulario con los datos del vehículo. Añade fotos de buena calidad y una descripción detallada para atraer más compradores.',
    category: 'selling',
  },
  {
    id: 'sell-2',
    question: '¿Cuánto cuesta publicar?',
    answer: 'Las publicaciones básicas son gratuitas para usuarios individuales. Para destacar tu vehículo o acceder a funciones premium, ofrecemos paquetes desde RD$500. Los dealers tienen planes mensuales.',
    category: 'selling',
  },
  {
    id: 'sell-3',
    question: '¿Cuánto tiempo dura mi publicación?',
    answer: 'Las publicaciones básicas duran 30 días. Puedes renovarlas gratuitamente o actualizar a un plan premium para mayor visibilidad.',
    category: 'selling',
  },
  // Account
  {
    id: 'acc-1',
    question: '¿Cómo creo una cuenta?',
    answer: 'Haz clic en "Registrarse", ingresa tu email y crea una contraseña. También puedes registrarte con Google, Facebook o Apple para mayor comodidad.',
    category: 'account',
  },
  {
    id: 'acc-2',
    question: '¿Qué es la verificación KYC?',
    answer: 'KYC (Know Your Customer) es un proceso de verificación de identidad. Los vendedores verificados tienen un badge que genera más confianza con los compradores.',
    category: 'account',
  },
  {
    id: 'acc-3',
    question: '¿Cómo elimino mi cuenta?',
    answer: 'Ve a Configuración > Privacidad > Eliminar cuenta. Tendrás 30 días para cambiar de opinión antes de que se eliminen tus datos permanentemente.',
    category: 'account',
  },
  // Payments
  {
    id: 'pay-1',
    question: '¿Qué métodos de pago aceptan?',
    answer: 'Aceptamos tarjetas de crédito/débito (VISA, Mastercard, American Express) a través de AZUL y CardNET, PayPal, y transferencias bancarias.',
    category: 'payments',
  },
  {
    id: 'pay-2',
    question: '¿OKLA maneja el pago del vehículo?',
    answer: 'No, OKLA es una plataforma de anuncios. El pago del vehículo se realiza directamente entre comprador y vendedor. Recomendamos hacerlo en un lugar seguro.',
    category: 'payments',
  },
  // General
  {
    id: 'gen-1',
    question: '¿OKLA opera en todo el país?',
    answer: 'Sí, OKLA está disponible en toda la República Dominicana. Puedes filtrar vehículos por provincia y ciudad.',
    category: 'general',
  },
  {
    id: 'gen-2',
    question: '¿Cómo reporto un anuncio sospechoso?',
    answer: 'Cada anuncio tiene un botón "Reportar". Nuestro equipo revisa los reportes en 24-48 horas y toma acción si es necesario.',
    category: 'general',
  },
];

const CATEGORIES: { id: FAQCategory; label: string; icon: string }[] = [
  { id: 'buying', label: 'Comprar', icon: '🛒' },
  { id: 'selling', label: 'Vender', icon: '💰' },
  { id: 'account', label: 'Cuenta', icon: '👤' },
  { id: 'payments', label: 'Pagos', icon: '💳' },
  { id: 'general', label: 'General', icon: '📋' },
];

export default function FAQPage() {
  const { t } = useTranslation('faq');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<FAQCategory | 'all'>('all');
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

  // Filter FAQs
  const filteredFAQs = useMemo(() => {
    return FAQ_ITEMS.filter((faq) => {
      const matchesCategory = selectedCategory === 'all' || faq.category === selectedCategory;
      const matchesSearch =
        searchQuery === '' ||
        faq.question.toLowerCase().includes(searchQuery.toLowerCase()) ||
        faq.answer.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }, [searchQuery, selectedCategory]);

  // Group by category
  const groupedFAQs = useMemo(() => {
    if (selectedCategory !== 'all') {
      return { [selectedCategory]: filteredFAQs };
    }
    return filteredFAQs.reduce((acc, faq) => {
      if (!acc[faq.category]) acc[faq.category] = [];
      acc[faq.category].push(faq);
      return acc;
    }, {} as Record<FAQCategory, FAQItem[]>);
  }, [filteredFAQs, selectedCategory]);

  const toggleExpand = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50">
        {/* Hero */}
        <div className="bg-gradient-to-r from-blue-600 to-indigo-600 text-white py-16">
          <div className="max-w-4xl mx-auto px-4 text-center">
            <FiHelpCircle size={48} className="mx-auto mb-4 opacity-80" />
            <h1 className="text-4xl font-bold mb-4">
              {t('faq.title', 'Preguntas Frecuentes')}
            </h1>
            <p className="text-xl text-white/80 mb-8">
              {t('faq.subtitle', 'Encuentra respuestas a las preguntas más comunes')}
            </p>

            {/* Search */}
            <div className="max-w-xl mx-auto relative">
              <FiSearch className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" size={20} />
              <input
                type="text"
                placeholder={t('faq.searchPlaceholder', 'Buscar en las FAQ...')}
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-12 pr-4 py-3 rounded-xl text-gray-900 focus:ring-4 focus:ring-white/30"
              />
            </div>
          </div>
        </div>

        <div className="max-w-4xl mx-auto px-4 py-12">
          {/* Category Filters */}
          <div className="flex flex-wrap gap-2 mb-8 justify-center">
            <button
              onClick={() => setSelectedCategory('all')}
              className={`px-4 py-2 rounded-full font-medium transition-colors ${
                selectedCategory === 'all'
                  ? 'bg-blue-600 text-white'
                  : 'bg-white text-gray-700 hover:bg-gray-100'
              }`}
            >
              Todas
            </button>
            {CATEGORIES.map((cat) => (
              <button
                key={cat.id}
                onClick={() => setSelectedCategory(cat.id)}
                className={`px-4 py-2 rounded-full font-medium transition-colors ${
                  selectedCategory === cat.id
                    ? 'bg-blue-600 text-white'
                    : 'bg-white text-gray-700 hover:bg-gray-100'
                }`}
              >
                {cat.icon} {cat.label}
              </button>
            ))}
          </div>

          {/* FAQ List */}
          {Object.entries(groupedFAQs).map(([category, faqs]) => (
            <div key={category} className="mb-8">
              {selectedCategory === 'all' && (
                <h2 className="text-lg font-semibold text-gray-900 mb-4 capitalize">
                  {CATEGORIES.find((c) => c.id === category)?.icon}{' '}
                  {CATEGORIES.find((c) => c.id === category)?.label}
                </h2>
              )}

              <div className="space-y-3">
                {faqs.map((faq) => (
                  <div key={faq.id} className="bg-white rounded-xl shadow-sm overflow-hidden">
                    <button
                      onClick={() => toggleExpand(faq.id)}
                      className="w-full px-6 py-4 text-left flex items-center justify-between hover:bg-gray-50"
                    >
                      <span className="font-medium text-gray-900">{faq.question}</span>
                      {expandedIds.has(faq.id) ? (
                        <FiChevronUp className="text-gray-400 flex-shrink-0" />
                      ) : (
                        <FiChevronDown className="text-gray-400 flex-shrink-0" />
                      )}
                    </button>
                    {expandedIds.has(faq.id) && (
                      <div className="px-6 pb-4">
                        <p className="text-gray-600">{faq.answer}</p>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}

          {/* No Results */}
          {filteredFAQs.length === 0 && (
            <div className="text-center py-12">
              <FiHelpCircle size={48} className="mx-auto mb-4 text-gray-300" />
              <p className="text-gray-500">No encontramos resultados para tu búsqueda</p>
              <button
                onClick={() => {
                  setSearchQuery('');
                  setSelectedCategory('all');
                }}
                className="mt-4 text-blue-600 hover:underline"
              >
                Limpiar filtros
              </button>
            </div>
          )}

          {/* Contact CTA */}
          <div className="mt-12 bg-blue-50 border border-blue-200 rounded-xl p-8 text-center">
            <h3 className="text-xl font-semibold text-gray-900 mb-2">
              ¿No encontraste lo que buscabas?
            </h3>
            <p className="text-gray-600 mb-4">
              Nuestro equipo de soporte está listo para ayudarte
            </p>
            <a
              href="/help/contact"
              className="inline-block px-6 py-3 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700"
            >
              Contactar Soporte
            </a>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 🚀 HOWITWORKSPAGE

**Ruta:** `/how-it-works` o `/about/how-it-works`

```typescript
// src/pages/HowItWorksPage.tsx
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import MainLayout from '@/layouts/MainLayout';
import {
  FiSearch, FiMessageSquare, FiCheckCircle,
  FiCamera, FiUsers, FiDollarSign,
  FiShield, FiTruck, FiStar
} from 'react-icons/fi';

export default function HowItWorksPage() {
  const { t } = useTranslation('how-it-works');

  const buyerSteps = [
    {
      icon: <FiSearch size={32} />,
      title: 'Busca tu vehículo',
      description: 'Usa nuestros filtros avanzados para encontrar el vehículo perfecto. Filtra por marca, modelo, año, precio, kilometraje y ubicación.',
    },
    {
      icon: <FiMessageSquare size={32} />,
      title: 'Contacta al vendedor',
      description: 'Envía un mensaje, llama o usa WhatsApp para comunicarte directamente con el vendedor. Agenda una visita para ver el vehículo.',
    },
    {
      icon: <FiCheckCircle size={32} />,
      title: 'Compra con confianza',
      description: 'Inspecciona el vehículo, verifica los documentos y negocia el precio. Completa la transacción directamente con el vendedor.',
    },
  ];

  const sellerSteps = [
    {
      icon: <FiCamera size={32} />,
      title: 'Publica tu vehículo',
      description: 'Crea una cuenta y publica tu vehículo en minutos. Añade fotos de alta calidad y una descripción detallada.',
    },
    {
      icon: <FiUsers size={32} />,
      title: 'Recibe contactos',
      description: 'Los compradores interesados te contactarán directamente. Responde rápido para aumentar tus posibilidades de venta.',
    },
    {
      icon: <FiDollarSign size={32} />,
      title: 'Cierra la venta',
      description: 'Negocia con los compradores, acuerda un precio justo y completa la venta. Es así de simple.',
    },
  ];

  const features = [
    {
      icon: <FiShield size={28} />,
      title: 'Vendedores Verificados',
      description: 'Los dealers pasan por un proceso de verificación KYC para mayor confianza.',
    },
    {
      icon: <FiTruck size={28} />,
      title: 'Miles de Vehículos',
      description: 'La mayor selección de vehículos nuevos y usados en República Dominicana.',
    },
    {
      icon: <FiStar size={28} />,
      title: 'Reseñas Reales',
      description: 'Lee las opiniones de otros compradores antes de tomar una decisión.',
    },
  ];

  return (
    <MainLayout>
      <div className="min-h-screen">
        {/* Hero Section */}
        <div className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-800 text-white py-20">
          <div className="max-w-6xl mx-auto px-4 text-center">
            <h1 className="text-4xl md:text-5xl font-bold mb-6">
              {t('howItWorks.title', '¿Cómo funciona OKLA?')}
            </h1>
            <p className="text-xl text-white/80 max-w-2xl mx-auto">
              {t('howItWorks.subtitle', 'Comprar o vender tu vehículo nunca ha sido tan fácil. Conectamos compradores y vendedores en toda la República Dominicana.')}
            </p>
          </div>
        </div>

        {/* For Buyers Section */}
        <section className="py-20 bg-white">
          <div className="max-w-6xl mx-auto px-4">
            <div className="text-center mb-12">
              <span className="text-blue-600 font-medium">Para Compradores</span>
              <h2 className="text-3xl font-bold text-gray-900 mt-2">
                Encuentra tu próximo vehículo
              </h2>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              {buyerSteps.map((step, index) => (
                <div key={index} className="relative">
                  {/* Connector Line */}
                  {index < buyerSteps.length - 1 && (
                    <div className="hidden md:block absolute top-12 left-1/2 w-full h-0.5 bg-gray-200" />
                  )}

                  <div className="relative bg-white p-8 rounded-xl text-center z-10">
                    {/* Step Number */}
                    <div className="absolute -top-4 left-1/2 -translate-x-1/2 w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center font-bold text-sm">
                      {index + 1}
                    </div>

                    {/* Icon */}
                    <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-6 text-blue-600">
                      {step.icon}
                    </div>

                    <h3 className="text-xl font-semibold text-gray-900 mb-3">{step.title}</h3>
                    <p className="text-gray-600">{step.description}</p>
                  </div>
                </div>
              ))}
            </div>

            <div className="text-center mt-12">
              <Link
                to="/browse"
                className="inline-block px-8 py-3 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors"
              >
                Buscar vehículos
              </Link>
            </div>
          </div>
        </section>

        {/* For Sellers Section */}
        <section className="py-20 bg-gray-50">
          <div className="max-w-6xl mx-auto px-4">
            <div className="text-center mb-12">
              <span className="text-green-600 font-medium">Para Vendedores</span>
              <h2 className="text-3xl font-bold text-gray-900 mt-2">
                Vende tu vehículo rápidamente
              </h2>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              {sellerSteps.map((step, index) => (
                <div key={index} className="relative">
                  {/* Connector Line */}
                  {index < sellerSteps.length - 1 && (
                    <div className="hidden md:block absolute top-12 left-1/2 w-full h-0.5 bg-gray-300" />
                  )}

                  <div className="relative bg-white p-8 rounded-xl text-center shadow-sm z-10">
                    {/* Step Number */}
                    <div className="absolute -top-4 left-1/2 -translate-x-1/2 w-8 h-8 bg-green-600 text-white rounded-full flex items-center justify-center font-bold text-sm">
                      {index + 1}
                    </div>

                    {/* Icon */}
                    <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6 text-green-600">
                      {step.icon}
                    </div>

                    <h3 className="text-xl font-semibold text-gray-900 mb-3">{step.title}</h3>
                    <p className="text-gray-600">{step.description}</p>
                  </div>
                </div>
              ))}
            </div>

            <div className="text-center mt-12">
              <Link
                to="/sell"
                className="inline-block px-8 py-3 bg-green-600 text-white rounded-lg font-medium hover:bg-green-700 transition-colors"
              >
                Publicar vehículo
              </Link>
            </div>
          </div>
        </section>

        {/* Features Section */}
        <section className="py-20 bg-white">
          <div className="max-w-6xl mx-auto px-4">
            <div className="text-center mb-12">
              <h2 className="text-3xl font-bold text-gray-900">
                ¿Por qué elegir OKLA?
              </h2>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              {features.map((feature, index) => (
                <div
                  key={index}
                  className="p-6 border rounded-xl hover:shadow-lg transition-shadow"
                >
                  <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center text-blue-600 mb-4">
                    {feature.icon}
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">{feature.title}</h3>
                  <p className="text-gray-600">{feature.description}</p>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* CTA Section */}
        <section className="py-20 bg-gradient-to-r from-blue-600 to-indigo-600 text-white">
          <div className="max-w-4xl mx-auto px-4 text-center">
            <h2 className="text-3xl font-bold mb-4">¿Listo para comenzar?</h2>
            <p className="text-xl text-white/80 mb-8">
              Únete a miles de dominicanos que ya compraron o vendieron su vehículo en OKLA
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Link
                to="/register"
                className="px-8 py-3 bg-white text-blue-600 rounded-lg font-medium hover:bg-gray-100 transition-colors"
              >
                Crear cuenta gratis
              </Link>
              <Link
                to="/browse"
                className="px-8 py-3 border border-white/30 text-white rounded-lg font-medium hover:bg-white/10 transition-colors"
              >
                Ver vehículos
              </Link>
            </div>
          </div>
        </section>
      </div>
    </MainLayout>
  );
}
```

---

## ⭐ WRITEREVIEWPAGE

**Ruta:** `/reviews/write/:sellerId` o `/write-review/:sellerId`

```typescript
// src/pages/WriteReviewPage.tsx
import { useState } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import MainLayout from '@/layouts/MainLayout';
import { FiStar, FiCheck, FiArrowLeft } from 'react-icons/fi';
import { reviewService } from '@/services/reviewService';
import Button from '@/components/atoms/Button';

interface ReviewFormData {
  rating: number;
  title: string;
  content: string;
  pros: string;
  cons: string;
  wouldRecommend: boolean;
}

export default function WriteReviewPage() {
  const { t } = useTranslation('reviews');
  const { sellerId } = useParams<{ sellerId: string }>();
  const [searchParams] = useSearchParams();
  const vehicleId = searchParams.get('vehicleId');
  const navigate = useNavigate();

  const [formData, setFormData] = useState<ReviewFormData>({
    rating: 0,
    title: '',
    content: '',
    pros: '',
    cons: '',
    wouldRecommend: true,
  });
  const [hoverRating, setHoverRating] = useState(0);
  const [isSuccess, setIsSuccess] = useState(false);

  // Submit mutation
  const submitMutation = useMutation({
    mutationFn: (data: ReviewFormData) =>
      reviewService.createReview({
        sellerId: sellerId!,
        vehicleId: vehicleId || undefined,
        ...data,
      }),
    onSuccess: () => {
      setIsSuccess(true);
      // Redirect after 3 seconds
      setTimeout(() => {
        navigate(vehicleId ? `/vehicles/${vehicleId}` : `/sellers/${sellerId}`);
      }, 3000);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (formData.rating === 0) return;
    submitMutation.mutate(formData);
  };

  const handleChange = (field: keyof ReviewFormData, value: string | number | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  // Success State
  if (isSuccess) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4">
          <div className="max-w-md w-full text-center">
            <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <FiCheck size={40} className="text-green-600" />
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-4">
              ¡Gracias por tu reseña!
            </h1>
            <p className="text-gray-600 mb-8">
              Tu opinión ayuda a otros compradores a tomar mejores decisiones.
              Serás redirigido en unos segundos...
            </p>
            <Button onClick={() => navigate(-1)} variant="outline">
              Volver
            </Button>
          </div>
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="max-w-2xl mx-auto px-4">
          {/* Back Button */}
          <button
            onClick={() => navigate(-1)}
            className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-6"
          >
            <FiArrowLeft size={20} />
            Volver
          </button>

          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">
              {t('reviews.write.title', 'Escribir una reseña')}
            </h1>
            <p className="text-gray-600 mt-2">
              {t('reviews.write.subtitle', 'Comparte tu experiencia con otros compradores')}
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm p-8">
            {/* Rating */}
            <div className="mb-8">
              <label className="block text-sm font-medium text-gray-700 mb-3">
                Calificación general *
              </label>
              <div className="flex items-center gap-2">
                {[1, 2, 3, 4, 5].map((star) => (
                  <button
                    key={star}
                    type="button"
                    onClick={() => handleChange('rating', star)}
                    onMouseEnter={() => setHoverRating(star)}
                    onMouseLeave={() => setHoverRating(0)}
                    className="p-1 focus:outline-none"
                  >
                    <FiStar
                      size={32}
                      className={`transition-colors ${
                        star <= (hoverRating || formData.rating)
                          ? 'text-yellow-400 fill-yellow-400'
                          : 'text-gray-300'
                      }`}
                    />
                  </button>
                ))}
                <span className="ml-2 text-gray-500">
                  {formData.rating > 0 ? `${formData.rating}/5` : 'Selecciona'}
                </span>
              </div>
            </div>

            {/* Title */}
            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Título de tu reseña *
              </label>
              <input
                type="text"
                value={formData.title}
                onChange={(e) => handleChange('title', e.target.value)}
                placeholder="Resume tu experiencia en una frase"
                className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                required
                maxLength={100}
              />
            </div>

            {/* Content */}
            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Tu reseña *
              </label>
              <textarea
                value={formData.content}
                onChange={(e) => handleChange('content', e.target.value)}
                placeholder="Cuéntanos sobre tu experiencia comprando o tratando con este vendedor..."
                rows={5}
                className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 resize-none"
                required
                minLength={50}
                maxLength={1000}
              />
              <p className="text-xs text-gray-500 mt-1">
                {formData.content.length}/1000 caracteres (mínimo 50)
              </p>
            </div>

            {/* Pros */}
            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Lo que más te gustó (opcional)
              </label>
              <textarea
                value={formData.pros}
                onChange={(e) => handleChange('pros', e.target.value)}
                placeholder="Ej: Buen trato, respuesta rápida, vehículo en excelente estado..."
                rows={2}
                className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 resize-none"
                maxLength={500}
              />
            </div>

            {/* Cons */}
            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Lo que se podría mejorar (opcional)
              </label>
              <textarea
                value={formData.cons}
                onChange={(e) => handleChange('cons', e.target.value)}
                placeholder="Ej: Demora en responder, documentos incompletos..."
                rows={2}
                className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 resize-none"
                maxLength={500}
              />
            </div>

            {/* Would Recommend */}
            <div className="mb-8">
              <label className="block text-sm font-medium text-gray-700 mb-3">
                ¿Recomendarías a este vendedor?
              </label>
              <div className="flex gap-4">
                <button
                  type="button"
                  onClick={() => handleChange('wouldRecommend', true)}
                  className={`flex-1 py-3 px-4 rounded-lg border-2 font-medium transition-colors ${
                    formData.wouldRecommend
                      ? 'border-green-500 bg-green-50 text-green-700'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  👍 Sí, lo recomiendo
                </button>
                <button
                  type="button"
                  onClick={() => handleChange('wouldRecommend', false)}
                  className={`flex-1 py-3 px-4 rounded-lg border-2 font-medium transition-colors ${
                    !formData.wouldRecommend
                      ? 'border-red-500 bg-red-50 text-red-700'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  👎 No lo recomiendo
                </button>
              </div>
            </div>

            {/* Error Message */}
            {submitMutation.isError && (
              <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
                Error al enviar la reseña. Por favor intenta de nuevo.
              </div>
            )}

            {/* Submit Button */}
            <Button
              type="submit"
              className="w-full"
              isLoading={submitMutation.isPending}
              disabled={formData.rating === 0 || !formData.title || formData.content.length < 50}
            >
              Publicar reseña
            </Button>

            <p className="text-xs text-gray-500 mt-4 text-center">
              Al publicar, aceptas nuestras políticas de contenido. Las reseñas son
              moderadas y pueden tardar hasta 24 horas en aparecer.
            </p>
          </form>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 🔧 SERVICIOS

```typescript
// src/services/reviewService.ts
import api from "./api";

export interface CreateReviewDto {
  sellerId: string;
  vehicleId?: string;
  rating: number;
  title: string;
  content: string;
  pros?: string;
  cons?: string;
  wouldRecommend: boolean;
}

export interface Review {
  id: string;
  sellerId: string;
  buyerId: string;
  buyerName: string;
  vehicleId?: string;
  vehicleTitle?: string;
  rating: number;
  title: string;
  content: string;
  pros?: string;
  cons?: string;
  wouldRecommend: boolean;
  createdAt: string;
  helpful: number;
  verified: boolean;
}

export const reviewService = {
  // Create a review
  createReview: async (dto: CreateReviewDto): Promise<Review> => {
    const response = await api.post("/api/reviews", dto);
    return response.data;
  },

  // Get reviews for a seller
  getSellerReviews: async (sellerId: string): Promise<Review[]> => {
    const response = await api.get(`/api/reviews/seller/${sellerId}`);
    return response.data;
  },

  // Get reviews for a vehicle
  getVehicleReviews: async (vehicleId: string): Promise<Review[]> => {
    const response = await api.get(`/api/reviews/vehicle/${vehicleId}`);
    return response.data;
  },

  // Mark review as helpful
  markHelpful: async (reviewId: string): Promise<void> => {
    await api.post(`/api/reviews/${reviewId}/helpful`);
  },

  // Report review
  reportReview: async (reviewId: string, reason: string): Promise<void> => {
    await api.post(`/api/reviews/${reviewId}/report`, { reason });
  },
};
```

---

## ✅ VALIDACIÓN

### FAQPage

- [ ] Hero con search bar visible
- [ ] Filtros de categoría funcionan
- [ ] Click en "Todas" muestra todas las FAQs
- [ ] Search filtra por pregunta y respuesta
- [ ] Accordion expand/collapse funciona
- [ ] Empty state cuando no hay resultados
- [ ] CTA "Contactar Soporte" visible
- [ ] FAQs agrupadas por categoría

### HowItWorksPage

- [ ] Hero section visible
- [ ] 3 pasos para compradores con números
- [ ] 3 pasos para vendedores con números
- [ ] Connector lines entre pasos (desktop)
- [ ] 3 features "Por qué elegir OKLA"
- [ ] CTAs "Buscar vehículos" y "Publicar vehículo"
- [ ] CTA final "Crear cuenta gratis"

### WriteReviewPage

- [ ] Star rating clickeable (1-5)
- [ ] Hover effect en estrellas
- [ ] Título con max 100 chars
- [ ] Content con min 50 / max 1000 chars
- [ ] Character counter visible
- [ ] Pros/Cons opcionales
- [ ] Toggle "Recomendarías" funciona
- [ ] Botón submit disabled si falta rating/título/content
- [ ] Success state con checkmark
- [ ] Redirect después de 3 segundos
- [ ] Error state visible si falla

---

## 🔗 RUTAS

```typescript
// src/App.tsx
<Route path="/faq" element={<FAQPage />} />
<Route path="/help/faq" element={<Navigate to="/faq" replace />} />
<Route path="/how-it-works" element={<HowItWorksPage />} />
<Route path="/about/how-it-works" element={<Navigate to="/how-it-works" replace />} />
<Route path="/reviews/write/:sellerId" element={<ProtectedRoute><WriteReviewPage /></ProtectedRoute>} />
<Route path="/write-review/:sellerId" element={<Navigate to="/reviews/write/:sellerId" replace />} />
```

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Static Pages", () => {
  test("FAQPage debe mostrar preguntas con acordeones", async ({ page }) => {
    await page.goto("/faq");
    await expect(page.getByTestId("faq-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /preguntas frecuentes/i }),
    ).toBeVisible();
    await expect(page.getByTestId("faq-accordion").first()).toBeVisible();
  });

  test("FAQPage debe expandir respuestas al hacer clic", async ({ page }) => {
    await page.goto("/faq");
    await page.getByTestId("faq-accordion").first().click();
    await expect(page.getByTestId("faq-answer").first()).toBeVisible();
  });

  test("FAQPage debe filtrar por búsqueda", async ({ page }) => {
    await page.goto("/faq");
    await page.getByTestId("faq-search").fill("pago");
    const faqs = page.getByTestId("faq-accordion");
    await expect(faqs.first()).toContainText(/pago/i);
  });

  test("HowItWorksPage debe mostrar pasos del proceso", async ({ page }) => {
    await page.goto("/how-it-works");
    await expect(page.getByTestId("how-it-works-page")).toBeVisible();
    await expect(
      page.getByRole("heading", { name: /cómo funciona/i }),
    ).toBeVisible();
    await expect(page.getByTestId("step-1")).toBeVisible();
    await expect(page.getByTestId("step-2")).toBeVisible();
    await expect(page.getByTestId("step-3")).toBeVisible();
  });

  test("WriteReviewPage debe requerir autenticación", async ({ page }) => {
    await page.goto("/reviews/write/seller-123");
    await expect(page).toHaveURL(/\/login/);
  });

  test("WriteReviewPage debe permitir enviar reseña", async ({ page }) => {
    await loginAsUser(page);
    await page.goto("/reviews/write/seller-123");
    await expect(page.getByTestId("write-review-page")).toBeVisible();
    await page.getByTestId("rating-stars").getByTestId("star-5").click();
    await page
      .getByTestId("review-text")
      .fill("Excelente vendedor, muy profesional.");
    await page.getByRole("button", { name: /enviar reseña/i }).click();
    await expect(page.getByText(/reseña enviada/i)).toBeVisible();
  });
});
```

---

_Última actualización: Enero 2026_
